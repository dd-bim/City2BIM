using City2BIM.Geometry;
using City2BIM.Logging;
using City2BIM.Semantic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;


namespace City2BIM.CityGML
{
    public class CityGMLReader
    {
        private C2BPoint lowerCornerPt;
        private Dictionary<string, XNamespace> allns;
        private HashSet<Xml_AttrRep> attributes = new HashSet<Xml_AttrRep>();
        private List<CityGml_Bldg> gmlBuildings = new List<CityGml_Bldg>();
        private string gmlCRS = "";

        public C2BPoint LowerCornerPt { get => lowerCornerPt; }
        public HashSet<Xml_AttrRep> Attributes { get => attributes; }
        public List<CityGml_Bldg> GmlBuildings { get => gmlBuildings; }
        public string GmlCRS { get => gmlCRS; }

        // The main Execute method (inherited from IExternalCommand) must be public

        /// <summary>
        /// Initializes import and calculation of CityGML Buildings
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="equalPtDist"></param>
        /// <param name="maxAngleBwPlaneNormals"></param>
        /// <param name="maxDevBwPlaneVerts"></param>
        public CityGMLReader(XDocument gmlDoc, bool? solid, double? equalPtDist = null, double? maxAngleBwPlaneNormals = null, double? maxDevBwPlaneVerts = null)
        {

            #region Namespaces

            //Save all namespaces in Dictionary with shortenings
            allns = gmlDoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration).
                GroupBy(a => a.Name.Namespace == XNamespace.None ? string.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                ToDictionary(g => g.Key, g => g.First());

            //special case:
            if (allns.ContainsKey(""))
            {
                if (!allns.ContainsKey("core"))
                    allns.Add("core", allns[""]);     //if namespace has no shortener --> core namespace is used
            }

            #endregion Namespaces

            #region CRS-Envelope

            var envelope = gmlDoc.Descendants(allns["gml"] + "Envelope").FirstOrDefault();

            var srsName = envelope.Attribute("srsName");
            gmlCRS = srsName.Value;

            #endregion CRS-Envelope

            //Main reading class for surface attributes and surface geometry:
            gmlBuildings = ReadGmlData(gmlDoc);

            //If user wishes to calculate solid geometry (topological correct, "watertight")
            if (solid.HasValue)
            {
                if ((bool)solid)
                {
                    //important settings to be made for calculation:
                    #region SettingsForSolidCalculation

                    //within this distance points are assumed as one vertex (one position)
                    if (equalPtDist.HasValue)
                    {
                        City2BIM_prop.EqualPtSq = (double)(equalPtDist * equalPtDist);
                    }
                    else
                        City2BIM_prop.EqualPtSq = 0.000001; //default value (distance = 1mm , squared)

                    //lesser than this angle (in degree) between the normal angles of surface planes, this planes are assumed as the same mathematical plane 
                    if (maxAngleBwPlaneNormals.HasValue)
                    {
                        City2BIM_prop.EqualPlSq = 2 - 2 * Math.Cos((double)maxAngleBwPlaneNormals * Math.PI / 180); //Kosinussatz für Schenkel mit 1m Länge (normalized normals)
                    }
                    else
                        City2BIM_prop.EqualPlSq = 0.0025; //default value (corresponds to 2.86°, 5cm bw normalized Normals (l=1m), squared))

                    //lesser than this distance the result of the level cut calculation will be accepted 
                    //(ATTENTION: currently there are some exceptional cases, see implementation at C2BSolid)
                    if (maxDevBwPlaneVerts.HasValue)
                    {
                        City2BIM_prop.MaxDevPlaneCutSq = (double)(maxDevBwPlaneVerts * maxDevBwPlaneVerts);
                    }
                    else
                        City2BIM_prop.MaxDevPlaneCutSq = 0.0025; //default value (distance = 5cm, 5cm bw normalized Normals (l=1m), squared))

                    #endregion SettingsForSolidCalculation

                    //main method for calculation
                    gmlBuildings = CalculateSolids(gmlBuildings);   //Creation of Solids
                }
            }
        }

        /// <summary>
        /// Splitting of PosList for XYZ coordinates
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="points">posList polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points (XYZ)</returns>
        private List<C2BPoint> CollectPoints(XElement points, C2BPoint lowerCorner)
        {
            string pointString = points.Value;
            var pointsSeperated = pointString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var polygonVertices = new List<C2BPoint>();

            for (int i = 0; i < pointsSeperated.Length; i += 3)
            {
                var coord = SplitCoordinate(new string[] { pointsSeperated[i], pointsSeperated[i + 1], pointsSeperated[i + 2] }, lowerCorner);

                polygonVertices.Add(coord);
            }
            return polygonVertices;
        }

        /// <summary>
        /// Splitting of Pos for single XYZ coordinate
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="position">pos tag</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>Polygon Point (XYZ)</returns>
        private C2BPoint CollectPoint(XElement position, C2BPoint lowerCorner)
        {
            var pointSeperated = position.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return SplitCoordinate(pointSeperated, lowerCorner);
        }

        private bool CheckSameStartEnd(List<C2BPoint> rawPolygon)
        {
            if (rawPolygon.First().X != rawPolygon.Last().X)
                return false;

            if (rawPolygon.First().Y != rawPolygon.Last().Y)
                return false;

            if (rawPolygon.First().Z != rawPolygon.Last().Z)
                return false;

            return true;
        }

        private bool CheckNumberOfPoints(List<C2BPoint> redPolygon)
        {
            if (redPolygon.Count < 4)
                return false;

            return true;
        }

        private List<C2BPoint> CheckDuplicates(List<C2BPoint> redPolygon)
        {
            List<C2BPoint> duplPts = new List<C2BPoint>();

            for (int i = 1; i < redPolygon.Count; i++)
            {
                if (redPolygon[i - 1].X != redPolygon[i].X)
                    continue;

                if (redPolygon[i - 1].Y != redPolygon[i].Y)
                    continue;

                if (redPolygon[i - 1].Z != redPolygon[i].Z)
                    continue;

                duplPts.Add(redPolygon[i]);
            }
            foreach (var dupl in duplPts)
            {
                redPolygon.Remove(dupl);
            }
            return redPolygon;
        }

        private List<C2BPoint> CheckForDeadEndAtAxisPlane(List<C2BPoint> rawPolygonSE, char fixedAxis)       //prüft nur über Koordinatenkomponenetenvergleich und nur für Fall gleiche XY-Koordinaten
        {
            //avoids this:          (dead end will be removed, works only for points with same XY coordinates, dead end along Z axis)
            //       _______      
            //      |       |
            //      |       |
            //      |_______|_________

            List<C2BPoint> deletePt = new List<C2BPoint>();

            var rawPolygon = new List<C2BPoint>();
            rawPolygon.AddRange(rawPolygonSE);
            rawPolygon.RemoveAt(0);

            switch (fixedAxis)
            {
                case 'x':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.X, 2));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }

                case 'y':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.Y));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }

                case 'z':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.Z));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }
            }

            for (int i = 0; i < rawPolygon.Count; i++)
            {
                C2BPoint curr = rawPolygon[i];
                C2BPoint next = rawPolygon[0];
                C2BPoint upperNext = rawPolygon[0];

                if (i < rawPolygon.Count - 2)
                {
                    next = rawPolygon[i + 1];
                    upperNext = rawPolygon[i + 2];
                }
                else if (i == rawPolygon.Count - 2)
                {
                    next = rawPolygon[i + 1];
                    upperNext = rawPolygon[0];
                }
                else
                {
                    next = rawPolygon[0];
                    upperNext = rawPolygon[1];
                }

                if (fixedAxis == 'x')
                {
                    if (curr.X != upperNext.X)         //check to proceed quickly
                        continue;

                    if (curr.X != next.X)         //check to proceed quickly
                        continue;
                }

                if (fixedAxis == 'y')
                {
                    if (curr.Y != upperNext.Y)         //check to proceed quickly
                        continue;

                    if (curr.Y != next.Y)         //check to proceed quickly
                        continue;
                }

                if (fixedAxis == 'z')
                {
                    if (curr.Z != upperNext.Z)         //check to proceed quickly
                        continue;

                    if (curr.Z != next.Z)         //check to proceed quickly
                        continue;
                }

                var d12 = C2BPoint.DistanceSq(curr, next);
                var d23 = C2BPoint.DistanceSq(next, upperNext);
                var d13 = C2BPoint.DistanceSq(curr, upperNext);

                if (d13 > d12 && d13 > d23)     //in this case no dead end
                    continue;

                if (d13 < 0.001 || d12 < 0.001 || d23 < 0.001)
                    continue;

                var delPts = from r in rawPolygonSE
                             where r.X == next.X && r.Y == next.Y && r.Z == next.Z
                             select r;

                deletePt.AddRange(delPts);                    //second point is dead end --> will be removed later
            }
            return deletePt;
        }

        private C2BPoint SplitCoordinate(string[] xyzString, C2BPoint lowerCorner)
        {
            double z = double.Parse(xyzString[2], CultureInfo.InvariantCulture) - lowerCorner.Z;

            //Left-handed (geodetic) vs. right-handed (mathematical) system

            double axis0 = double.Parse(xyzString[0], CultureInfo.InvariantCulture);
            double axis1 = double.Parse(xyzString[1], CultureInfo.InvariantCulture);

            return new C2BPoint(axis0 - lowerCorner.X, axis1 - lowerCorner.Y, z);
        }

        private List<CityGml_Surface> ReadSurfaces(XElement bldgEl, HashSet<Xml_AttrRep> attributes, out CityGml_Bldg.LodRep lod)
        {
            var bldgParts = bldgEl.Elements(allns["bldg"] + "consistsOfBuildingPart");
            var bldg = bldgEl.Elements().Except(bldgParts);

            var surfaces = new List<CityGml_Surface>();

            bool poly = bldg.Descendants().Where(l => l.Name.LocalName.Contains("Polygon")).Any();

            if (!poly)                  //no polygons --> directly return (e.g. Buildings with Parts but no geometry at building level)
            {
                lod = CityGml_Bldg.LodRep.unknown;
                return surfaces;
            }
            bool lod2 = bldg.DescendantsAndSelf().Where(l => l.Name.LocalName.Contains("lod2")).Count() > 0;
            bool lod1 = bldg.DescendantsAndSelf().Where(l => l.Name.LocalName.Contains("lod1")).Count() > 0;

            if (lod2)
            {
                lod = CityGml_Bldg.LodRep.LOD2;

                #region WallSurfaces

                var lod2Walls = bldg.DescendantsAndSelf(allns["bldg"] + "WallSurface");

                foreach (var wall in lod2Walls)
                {
                    List<CityGml_Surface> wallSurface = ReadSurfaceType(wall, CityGml_Surface.FaceType.wall);
                    if (wallSurface == null)
                        return null;

                    surfaces.AddRange(wallSurface);
                }

                #endregion WallSurfaces

                #region RoofSurfaces

                var lod2Roofs = bldg.DescendantsAndSelf(allns["bldg"] + "RoofSurface");

                foreach (var roof in lod2Roofs)
                {
                    List<CityGml_Surface> roofSurface = ReadSurfaceType(roof, CityGml_Surface.FaceType.roof);
                    surfaces.AddRange(roofSurface);
                }

                #endregion RoofSurfaces

                #region GroundSurfaces

                var lod2Grounds = bldg.DescendantsAndSelf(allns["bldg"] + "GroundSurface");

                foreach (var ground in lod2Grounds)
                {
                    List<CityGml_Surface> groundSurface = ReadSurfaceType(ground, CityGml_Surface.FaceType.ground);
                    surfaces.AddRange(groundSurface);
                }

                #endregion GroundSurfaces

                #region ClosureSurfaces

                var lod2Closures = bldg.DescendantsAndSelf(allns["bldg"] + "ClosureSurface");

                foreach (var closure in lod2Closures)
                {
                    List<CityGml_Surface> closureSurface = ReadSurfaceType(closure, CityGml_Surface.FaceType.closure);
                    surfaces.AddRange(closureSurface);
                }

                #endregion ClosureSurfaces

                #region OuterCeilingSurfaces

                var lod2OuterCeiling = bldg.DescendantsAndSelf(allns["bldg"] + "OuterCeilingSurface");

                foreach (var ceiling in lod2OuterCeiling)
                {
                    List<CityGml_Surface> outerCeilingSurface = ReadSurfaceType(ceiling, CityGml_Surface.FaceType.outerCeiling);
                    surfaces.AddRange(outerCeilingSurface);
                }

                #endregion OuterCeilingSurfaces

                #region OuterFloorSurfaces

                var lod2OuterFloor = bldg.DescendantsAndSelf(allns["bldg"] + "OuterFloorSurface");

                foreach (var floor in lod2OuterFloor)
                {
                    List<CityGml_Surface> outerFloorSurface = ReadSurfaceType(floor, CityGml_Surface.FaceType.outerFloor);
                    surfaces.AddRange(outerFloorSurface);
                }

                #endregion OuterFloorSurfaces
            }
            else if (lod1)
            {
                #region lod1Surfaces

                lod = CityGml_Bldg.LodRep.LOD1;

                //one occurence per building
                var lod1Rep = bldg.DescendantsAndSelf(allns["bldg"] + "lod1Solid").FirstOrDefault();

                if (lod1Rep == null)
                    lod1Rep = bldg.DescendantsAndSelf(allns["bldg"] + "lod1MultiSurface").FirstOrDefault();

                if (lod1Rep != null)
                {
                    var polys = lod1Rep.Descendants(allns["gml"] + "Polygon").ToList();
                    var elemsWithID = lod1Rep.DescendantsAndSelf().Where(a => a.Attribute(allns["gml"] + "id") != null);

                    for (var i = 0; i < polys.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                    {
                        var surface = new CityGml_Surface();

                        var faceID = polys[i].Attribute(allns["gml"] + "id").Value;

                        if (faceID == null)
                        {
                            var gmlSolid = lod1Rep.Descendants(allns["gml"] + "Solid").FirstOrDefault();

                            faceID = gmlSolid.Attribute(allns["gml"] + "id").Value;

                            if (faceID == null)
                            {
                                faceID = bldgEl.Attribute(allns["gml"] + "id").Value + "_" + i;
                            }
                        }

                        surface.Facetype = CityGml_Surface.FaceType.unknown;
                        surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(polys[i], attributes, CityGml_Surface.FaceType.unknown);

                        var surfacePl = ReadSurfaceData(polys[i], surface);

                        surfaces.Add(surfacePl);
                    }
                }

                #endregion lod1Surfaces
            }
            else
                lod = CityGml_Bldg.LodRep.unknown;

            return surfaces;
        }

        private List<CityGml_Surface> ReadSurfaceType(XElement gmlSurface, CityGml_Surface.FaceType type)
        {
            List<CityGml_Surface> polyList = new List<CityGml_Surface>();

            var polysR = gmlSurface.Descendants(allns["gml"] + "Polygon").ToList();

            for (var i = 0; i < polysR.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
            {
                var surface = new CityGml_Surface();

                var faceID = polysR[i].Attribute(allns["gml"] + "id");

                if (faceID != null)
                    surface.SurfaceId = faceID.Value;

                surface.Facetype = type;
                surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(gmlSurface, Attributes, type);

                var surfacePl = ReadSurfaceData(polysR[i], surface);
                if (surfacePl == null)
                    return null;

                polyList.Add(surfacePl);
            }
            return polyList;
        }

        /// <summary>
        /// Reads exterior and interior polygon data
        /// </summary>
        /// <param name="poly">GML Polygon element</param>
        /// <returns></returns>
        private CityGml_Surface ReadSurfaceData(XElement poly, CityGml_Surface rawFace)
        {
            var surface = new CityGml_Surface
            {
                SurfaceId = rawFace.SurfaceId,
                SurfaceAttributes = rawFace.SurfaceAttributes,
                Facetype = rawFace.Facetype
            };

            #region ExteriorPolygon

            //only one could (should) exist

            var exteriorF = poly.Descendants(allns["gml"] + "exterior").FirstOrDefault();

            var posListExt = exteriorF.Descendants(allns["gml"] + "posList");

            var ptList = new List<C2BPoint>();

            if (posListExt.Any())
            {
                ptList.AddRange(CollectPoints(posListExt.FirstOrDefault(), lowerCornerPt));
            }
            else
            {
                var posExt = exteriorF.Descendants(allns["gml"] + "pos");

                foreach (var pos in posExt)
                {
                    ptList.Add(CollectPoint(pos, lowerCornerPt));
                }
            }

            //-------------------Check section-----------------------

            //Start = End

            if (!CheckSameStartEnd(ptList))
                ptList.Add(ptList[0]);

            List<C2BPoint> pointsRed = ptList;
            //-------------

            var deletionXY = CheckForDeadEndAtAxisPlane(pointsRed, 'z');

            if (deletionXY.Count > 0)
            {
                bool addNewStart = false;

                if (deletionXY.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionXY).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            var deletionXZ = CheckForDeadEndAtAxisPlane(pointsRed, 'y');

            if (deletionXZ.Count > 0)
            {
                bool addNewStart = false;

                if (deletionXZ.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionXZ).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            var deletionYZ = CheckForDeadEndAtAxisPlane(pointsRed, 'x');

            if (deletionYZ.Count > 0)
            {
                bool addNewStart = false;

                if (deletionYZ.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionYZ).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            //--------------

            //Duplicates:
            pointsRed = CheckDuplicates(pointsRed);

            //Too few points (possibly reduced point list will be investigated because dead end could be removed)
            if (!CheckNumberOfPoints(pointsRed))
                ptList.Clear();

            //-----------------------------------------------------------

            surface.ExteriorPts = pointsRed;

            #endregion ExteriorPolygon

            #region InteriorPolygon

            //if existent, it also could have more than one hole

            var interiorF = poly.Descendants(allns["gml"] + "interior");

            var posListInt = interiorF.Descendants(allns["gml"] + "posList").ToList();

            if (posListInt.Any())
            {
                List<List<C2BPoint>> interiorPolys = new List<List<C2BPoint>>();

                for (var j = 0; j < posListInt.Count(); j++)
                {
                    interiorPolys.Add(CollectPoints(posListInt[j], LowerCornerPt));
                }
                surface.InteriorPts = interiorPolys;
            }
            else
            {
                var rings = interiorF.Descendants(allns["gml"] + "LinearRing").ToList();

                List<List<C2BPoint>> interiorPolys = new List<List<C2BPoint>>();

                for (var k = 0; k < rings.Count() - 1; k++)
                {
                    var posInt = rings[k].Descendants(allns["gml"] + "pos");

                    if (posInt.Any())
                    {
                        var ptListI = new List<C2BPoint>();

                        foreach (var pos in posInt)
                        {
                            ptListI.Add(CollectPoint(pos, lowerCornerPt));
                        }
                        interiorPolys.Add(ptListI);
                    }
                }
                surface.InteriorPts = interiorPolys;
            }

            #endregion InteriorPolygon

            return surface;
        }

        public List<CityGml_Bldg> ReadGmlData(XDocument gmlDoc/*, DxfVisualizer dxf*/)
        {
            #region LowerCorner

            //For better calculation, Identify lower Corner
            var lowerCorner = gmlDoc.Descendants(allns["gml"] + "lowerCorner").FirstOrDefault();
            lowerCornerPt = CollectPoint(lowerCorner, new C2BPoint(0, 0, 0));

            #endregion LowerCorner

            //Read all overall building elements
            var gmlBuildings = gmlDoc.Descendants(allns["bldg"] + "Building");
            //--------------------------------------------------------------------------

            #region Semantics

            //Read all semantic attributes first:
            //Loop over all buildings, parameter list in Revit needs consistent parameters for object types
            //first of all regular schema attributes (inherited by parsing of XML schema for core and bldg, standard specific)
            attributes = City_Semantic.GetSchemaAttributes();

            //secondly add generic attributes (file specific)
            var genAttr = City_Semantic.ReadGenericAttributes(gmlBuildings, allns);

            //union for consistent attribute list
            Attributes.UnionWith(genAttr);
            //--------------------------------------------------------------------------------------------------------------------------

            #endregion Semantics

            #region BuildingInstances

            //set up of individual building elements for overall list
            var gmlBldgs = new List<CityGml_Bldg>();

            foreach (var bldg in gmlBuildings)
            {
                //create instance of GmlBldg
                var gmlBldg = new CityGml_Bldg
                {
                    //use gml_id as id for building
                    BldgId = bldg.Attribute(allns["gml"] + "id").Value,

                    LogEntries = new List<LogPair>()
                };
                gmlBldg.LogEntries.Add(new LogPair(LogType.info, "CityGML-Building_ID: " + gmlBldg.BldgId));

                //read attributes for building (first level, no parts are handled internally in method)
                gmlBldg.BldgAttributes = new ReadSemValues().ReadAttributeValuesBldg(bldg, Attributes, allns);

                var surfaces = new List<CityGml_Surface>();

                surfaces = ReadSurfaces(bldg, Attributes, out var lod);

                gmlBldg.Lod = lod.ToString();
                gmlBldg.BldgSurfaces = surfaces;

                //investigation of building parts
                var gmlBuildingParts = bldg.Descendants(allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 possible

                var parts = new List<CityGml_BldgPart>();

                foreach (var part in gmlBuildingParts)
                {
                    //create instace for building part
                    var gmlBldgPart = new CityGml_BldgPart
                    {
                        //use gml_id for building part
                        BldgPartId = part.Attribute(allns["gml"] + "id").Value,

                        //read attributes for building part
                        BldgPartAttributes = new ReadSemValues().ReadAttributeValuesBldg(part, Attributes, allns)
                    };

                    var partSurfaces = new List<CityGml_Surface>();

                    partSurfaces = ReadSurfaces(part, Attributes, out var lodP);

                    gmlBldgPart.Lod = lodP.ToString();
                    gmlBldgPart.PartSurfaces = partSurfaces;

                    parts.Add(gmlBldgPart);
                }
                //-------------------------------------------

                gmlBldg.Parts = parts;

                gmlBldgs.Add(gmlBldg);
            }

            #endregion BuildingInstances

            return gmlBldgs;
        }

        public List<CityGml_Bldg> CalculateSolids(List<CityGml_Bldg> bldgs)
        {
            var newBldgs = new List<CityGml_Bldg>();

            foreach (var bldg in bldgs)
            {
                bldg.BldgSolid = new C2BSolid(bldg.BldgId);

                var surfaces = bldg.BldgSurfaces;

                if (surfaces.Count > 0)
                {
                    foreach (var surface in surfaces)
                    {
                        bldg.BldgSolid.AddPlane(surface.InternalID.ToString(), surface.ExteriorPts, surface.InteriorPts);
                    }

                    bldg.BldgSolid.IdentifySimilarPlanes();

                    bldg.BldgSolid.CalculatePositions();

                    bldg.LogEntries.AddRange(bldg.BldgSolid.LogEntries);
                }

                var parts = bldg.Parts;

                var newParts = new List<CityGml_BldgPart>();

                foreach (var part in parts)
                {
                    var newPart = part;

                    var partSolid = new C2BSolid(part.BldgPartId);

                    foreach (var partSurface in newPart.PartSurfaces)
                    {
                        partSolid.AddPlane(partSurface.InternalID.ToString(), partSurface.ExteriorPts, partSurface.InteriorPts);
                    }
                    partSolid.IdentifySimilarPlanes();

                    partSolid.CalculatePositions();

                    bldg.LogEntries.Add(new LogPair(LogType.info, "CityGML-BuildingPart_ID: " + part.BldgPartId));

                    bldg.LogEntries.AddRange(partSolid.LogEntries);

                    newPart.PartSolid = partSolid;

                    newParts.Add(newPart);
                }

                bldg.Parts = newParts;

                newBldgs.Add(bldg);
            }
            return newBldgs;
        }
    }
}