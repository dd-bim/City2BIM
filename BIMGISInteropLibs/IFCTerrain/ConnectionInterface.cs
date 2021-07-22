using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Collections;                        //MESH - will be removed
using BimGisCad.Representation.Geometry;            //Axis2Placement3D
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...

//embed IFC
using BIMGISInteropLibs.IFC;    //IFC-Writer

//embed for Logging
using BIMGISInteropLibs.Logging; //logging
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//IxMilia: for processing dxf files
using IxMilia.Dxf;

namespace BIMGISInteropLibs.IfcTerrain
{
    public class ConnectionInterface
    {
        //initialize TIN / MESH / Breaklines
        /// <summary>
        /// MESH (result of the specific file reader)
        /// </summary>
        public Mesh Mesh { get; private set; }

        /// <summary>
        /// TIN (result of the specific file reader)
        /// </summary>
        public Tin Tin { get; private set; }

        /// <summary>
        /// breaklines (result of the specific file reader) [TODO]
        /// </summary>
        public Dictionary<int, Line3> Breaklines { get; private set; }

        /// <summary>
        /// ConnectionInterface between file reader and ifc writer
        /// </summary>
        public Result mapProcess(JsonSettings jSettings, JsonSettings_DIN_SPEC_91391_2 jSettings_DIN91931, JsonSettings_DIN_18740_6 jSettings_DIN18740, double? breakDist = null)
        {
            LogWriter.Add(LogType.info, "Processing-Protocol for IFCTerrain");
            LogWriter.Add(LogType.info, "--------------------------------------------------");
            LogWriter.Add(LogType.verbose, "Mapping process started.");
            
            //The processing is basically done by a reader and a writer (these are included in the corresponding regions)
            #region reader
            //initalize transfer class
            var result = new Result();

            #region import data via type selection
            //In the following a mapping is made on the basis of the data type, so that the respective reader is called up
            switch (jSettings.fileType)
            {
                //reader for LandXML
                case IfcTerrainFileType.LandXML:
                    //result = LandXML.ReaderTerrain(jSettings.is3D, jSettings.filePath, jSettings.minDist, jSettings.logFilePath, jSettings.verbosityLevel);
                    result = LandXML.ReaderTerrain.ReadTin(jSettings);
                    //end of the case so it is jumped after file reading (above) out of the switch function
                    break;

                //reader for CityGML
                case IfcTerrainFileType.CityGML:
                    result = CityGML.CityGMLReaderTerrain.ReadTin(jSettings);
                    break;

                //reader for DXF
                case IfcTerrainFileType.DXF:
                    //file Reader: output will be used to process terrain information
                    DXF.ReaderTerrain.ReadFile(jSettings.filePath, out DxfFile dxfFile);

                    LogWriter.Add(LogType.verbose, "dxf file readed layers: " + dxfFile.Layers.Count);

                    //loop for distinguishing whether it is a tin or not (processing via points and lines)
                    if (jSettings.isTin)
                    {
                        //Tin - Reader (if dxf file contains faces)
                        result = DXF.ReaderTerrain.ReadDxfTin(dxfFile, jSettings);
                    }
                    else if (jSettings.recalculateTin.GetValueOrDefault())
                    {
                        //TIN is recalculated by using an existing TIN and breaklines
                        result = DXF.ReaderTerrain.RecalculateTin(dxfFile, jSettings);

                        //After a TIN has been calculated the atribute 'isTin' becomes true
                        jSettings.isTin = true;
                    }
                    else if (jSettings.calculateTin.GetValueOrDefault())
                    {
                        //TIN is calculated from point data (in case breaklines are selected these are uses as well)
                        result = DXF.ReaderTerrain.CalculateTin(dxfFile, jSettings);

                        //After a TIN has been calculated the atribute 'isTin' becomes true
                        jSettings.isTin = true;
                    }
                    else
                    {
                        //processing points and lines (via mesh)
                        result = DXF.ReaderTerrain.ReadDxfPoly(dxfFile, jSettings);
                    }
                    break;

                //reader for REB    
                case IfcTerrainFileType.REB:
                    //REB file reader
                    REB.RebDaData rebData = REB.ReaderTerrain.ReadReb(jSettings.filePath);
                    if (jSettings.isTin)
                    {
                        //use REB data via processing with converter
                        result = REB.ReaderTerrain.ConvertRebToTin(rebData, jSettings);
                    }
                    else if (jSettings.calculateTin.GetValueOrDefault())
                    {
                        //Calculate TIN if calculateTin is set
                        result = REB.ReaderTerrain.CalculateTin(rebData, jSettings);

                        //After a TIN has been calculated the atribute 'isTin' becomes true
                        jSettings.isTin = true;
                    }
                    break;

                //reader for Elev.Grid: default processing result is mesh!
                case IfcTerrainFileType.Grid:
                    result = ElevationGrid.ReaderTerrain.ReadGrid(jSettings);
                    break;

                //reader for GRAFBAT
                case IfcTerrainFileType.Grafbat:
                    if (jSettings.isTin)
                    {
                        //reader for processing GRAFBAT as TIN via points and triangles 
                        result = GEOgraf.ReadOUT.ReadOutData(jSettings, out IReadOnlyDictionary<int, int> pointIndex2NumberMap, out IReadOnlyDictionary<int, int> triangleIndex2NumerMap);
                    }
                    /*
                    else
                    {
                        //[TODO]: Placeholder for two additional readers (points and lines; only points)
                    }
                    */
                    //break must be outside the loop (above)!
                    break;

                //reader for PostGIS
                case IfcTerrainFileType.PostGIS:
                    if (jSettings.isTin)
                    {
                        //A exisiting TIN is read and converted
                        result = PostGIS.ReaderTerrain.ReadPostGIS(jSettings);
                    }
                    if (jSettings.recalculateTin.GetValueOrDefault())
                    {
                        //A existing TIN is recalculated by using the triangle and line data
                        result = PostGIS.ReaderTerrain.RecalculateTin(jSettings);

                        //After a TIN has been calculated the atribute 'isTin' becomes true
                        jSettings.isTin = true;
                    }
                    else if (jSettings.calculateTin.GetValueOrDefault())
                    {
                        //A TIN of MultiPoint data is calculated (if choosen, line data is used as well)
                        result = PostGIS.ReaderTerrain.CalculateTin(jSettings);

                        //After a TIN has been calculated the atribute 'isTin' becomes true
                        jSettings.isTin = true;
                    }
                    break;
            }
            //so that from the reader (TIN, Error, Mesh) is passed to respective "classes"
            LogWriter.Add(LogType.debug, "Reading file completed.");
            
            //passing results from reader to local var 
            this.Tin = result.Tin;      //passing tin
            this.Mesh = result.Mesh;    //passing mesh

            //tin error handler
            if (jSettings.isTin)
            {
                if (this.Tin.NumTriangles.Equals(0))
                {
                    //log error message
                    LogWriter.Add(LogType.error, "[READER] no DTM could be read on the basis of the input data. The DTM is empty! - Processing canceled!");

                    //Messagebox
                    //[REWORK] MessageBox.Show("The TIN is empty! - Processing canceled!", "TIN is empty", MessageBoxButton.OK, MessageBoxImage.Error);

                    return result;
                }
            }
            
            //mesh error handler
            else if (!jSettings.isTin)
            {
                //log reading results
                LogWriter.Add(LogType.info, "MESH read: " + Mesh.Points.Count + " points; " + Mesh.FaceEdges.Count + " triangles;");
            }
            //if tin has been readed succesfully
            else
            {
                //log reading results
                LogWriter.Add(LogType.info, "TIN read: " + Tin.Points.Count + " points; " + Tin.NumTriangles + " triangles;");
            }
            #endregion import data via type selection
            #endregion reader


            //from here are the IFC writers
            #region writer

            //proeject name - Check if this has not been assigned by the user
            if (string.IsNullOrEmpty(jSettings.projectName))
            {
                //project name assigned as placeholder
                jSettings.projectName = "Name of project";
                //so that a user can also adjust it later on

                //logging
                LogWriter.Add(LogType.warning, "Project name have been set as placeholder.");
            }

            #region placement / georef
            //Placment / Georeferencing
            var writeInput = new WriteInput();
            writeInput.Placement = Axis2Placement3D.Standard;

            double originX = 0;
            double originY = 0;
            double originZ = 0;

            //query whether a user has assigned "coordinates"
            if (jSettings.customOrigin.Value)
            {
                originX = jSettings.xOrigin.Value;
                originY = jSettings.yOrigin.Value;
                originZ = jSettings.zOrigin.Value;

                LogWriter.Add(LogType.verbose, "Custom orgin has been set.");
            }
            
            //Alternative: determine the "BoundingBox", and define the center (x,y,z) as the coordinate origin.
            else if(jSettings.isTin)
            {
                //initialize the variables to describe them
                double MinX = 0;
                double MinY = 0;
                double MinZ = 0;
                double MaxX = 0;
                double MaxY = 0;
                double MaxZ = 0;

                //Query whether mesh is not empty
                if (Mesh != null)
                {
                    //Determine center point (x,y,z)
                    originX = (this.Mesh.MaxX + this.Mesh.MinX) / 2;
                    originY = (this.Mesh.MaxY + this.Mesh.MinY) / 2;
                    originZ = (this.Mesh.MaxZ + this.Mesh.MinZ) / 2;

                }
                //Proposal for BimGisCad.Composed (in MESH this already exists) [TODO]
                else
                {
                    int i = 0;
                    //pass through each point in the TIN
                    foreach (Point3 point in Tin.Points)
                    {
                        //here all points that are not the starting point
                        if (i > 0)
                        {
                            //it is checked whether the current point is larger or smaller than the previous maxima / minima
                            //should this be the case, this is set accordingly as a value
                            if (point.X < MinX) { MinX = point.X; }
                            if (point.X > MaxX) { MaxX = point.X; }
                            if (point.Y < MinY) { MinY = point.Y; }
                            if (point.Y > MaxY) { MaxY = point.Y; }
                            if (point.Z < MinZ) { MinZ = point.Z; }
                            if (point.Z > MaxZ) { MaxZ = point.Z; }
                        }
                        //initalization through first point
                        else
                        {
                            //Set minima based on the first point
                            MinX = point.X;
                            MinY = point.Y;
                            MinZ = point.Z;
                            //Set maxima based on the first point
                            MaxX = point.X;
                            MaxY = point.Y;
                            MaxZ = point.Z;
                        }
                        //count up for each "processed" point
                        i++;
                    }
                    //Determine center point (x,y,z)
                    originX = (MaxX + MinX) / 2;
                    originY = (MaxY + MinY) / 2;
                    originZ = (MaxZ + MinZ) / 2;

                    //logging
                    LogWriter.Add(LogType.verbose, "Project center point was calculated and set.");
                }
            }
            //case mesh (not needed to calculate min or max values)
            else if(!jSettings.isTin)
            {
                //get center point via readed mesh
                originX = (this.Mesh.MinX + this.Mesh.MaxX) / 2;
                originY = (this.Mesh.MinY + this.Mesh.MaxY) / 2;
                originZ = (this.Mesh.MinZ + this.Mesh.MaxZ) / 2;

                //logging
                LogWriter.Add(LogType.verbose, "Project center point was calculated and set.");
                
            }
            //set center point as placement
            writeInput.Placement.Location = Vector3.Create(originX, originY, originZ);
            LogWriter.Add(LogType.debug, "Project center: X= " + originX + "; Y= " + originY + "; Z= " + originZ);
            writeInput.Placement.RefDirection = Direction3.Create(utils.getRotationVector(jSettings.trueNorth.GetValueOrDefault())[0], utils.getRotationVector(jSettings.trueNorth.GetValueOrDefault())[1], 0, null);
            #endregion placement / georef

            #region shape representation
            //interface for IFC shape representation on the basis of which the Shape - Representation are controlled
            writeInput.SurfaceType = SurfaceType.TFS; //standard case
            //Query whether another shape representation is desired
            if (jSettings.surfaceType == SurfaceType.GCS)
            {
                //set shape repr. to GCS
                writeInput.SurfaceType = SurfaceType.GCS;
            }
            else if (jSettings.surfaceType == SurfaceType.SBSM)
            {
                //set shape repr. to SBSM
                writeInput.SurfaceType = SurfaceType.SBSM;
            }
            /* here is a dummy (placeholder for IfcTriangulatedFaceSet) [TODO] add as soon as Ifc4dot3 is supported by Xbim
            else if (jSettings.surfaceType == "TIN")
            {
                writeInput.SurfaceType = SurfaceType.TIN;
            }
            */

            //logging
            LogWriter.Add(LogType.verbose, "Writer is using shape representation: " + writeInput.SurfaceType.ToString());
            #endregion shape representation

            #region ifc xml / step file
            

            //query whether an XML file is to be written additionally (IFC4 only)
            if (jSettings.outFileType == IfcFileType.ifcXML)
            {
                writeInput.FileType = IfcFileType.ifcXML;
                LogWriter.Add(LogType.debug, "Using settings to output ifcXML");
            }
            else if (jSettings.outFileType == IfcFileType.ifcZip)
            {
                writeInput.FileType = IfcFileType.ifcZip;
                LogWriter.Add(LogType.debug, "Using settings to output ifcZIP");
            }
            else
            {
                //Standard file type to STEP (Reason: supported by IFC2x3 *AND* IFC4, etc.)
                writeInput.FileType = IfcFileType.Step;

                LogWriter.Add(LogType.debug, "Using settings to output STEP");
            }
            #endregion ifc xml / step file

            //Logging
            LogWriter.Add(LogType.debug, "Writing IFC with:");
            LogWriter.Add(LogType.debug, "--> IFC Version " + jSettings.outIFCType);
            LogWriter.Add(LogType.debug, "--> Surfacetype " + jSettings.surfaceType);
            LogWriter.Add(LogType.debug, "--> Filetype " + jSettings.fileType);

            //region for ifc writer control
            #region IFC2x3 writer
            
            //Query whether output IFC file should be version IFC2x3
            if (jSettings.outIFCType == IfcVersion.IFC2x3)
            {
                //model (file) let generate via the Writer
                var model = IFC.Ifc2x3.Store.CreateViaTin(
                    jSettings,
                    jSettings_DIN91931,
                    jSettings_DIN18740,
                    writeInput.Placement,
                    result,
                    writeInput.SurfaceType,
                    breakDist);

                //access to file writer 
                IFC.Ifc2x3.Store.WriteFile(model, jSettings);

                //logging file info
                LogWriter.Add(LogType.info, "IFC file writen: " + jSettings.destFileName);

                //logging stat
                double numPoints = (double)result.wPoints / (double)result.rPoints;
                double numFaces = (double)result.wFaces / (double)result.rFaces;
                LogWriter.Add(LogType.info, "There were " + result.wPoints + " points (" + Math.Round(numPoints * 100, 2) + " %) and " + result.wFaces + " Triangles (" + Math.Round(numFaces * 100, 2) + " %) processed.");

            }
            #endregion IFC2x3 writer (TIN)

            #region IFC4 writer (TIN)
            else if (jSettings.outIFCType == IfcVersion.IFC4)
            {
                //create IFC4 modell
                var model = jSettings.geoElement.GetValueOrDefault()
                    //create with geo element
                    ? IFC.Ifc4.Geo.Create(
                        jSettings,
                        jSettings.logeoref,
                        writeInput.Placement,
                        result,
                        writeInput.SurfaceType,
                        breakDist)
                    //create withOUT geo element
                    : IFC.Ifc4.Store.CreateViaTin(
                        jSettings,
                        jSettings_DIN91931,
                        jSettings_DIN18740,
                        jSettings.logeoref,
                        writeInput.Placement,
                        result,
                        writeInput.SurfaceType,
                        breakDist);

                //access to file writer
                IFC.Ifc4.Store.WriteFile(model, jSettings);
                
                //logging file info
                LogWriter.Add(LogType.info, "IFC file writen: " + jSettings.destFileName);

                //logging stat
                double numPoints = (double)result.wPoints / (double)result.rPoints;
                double numFaces = (double)result.wFaces / (double)result.rFaces;
                LogWriter.Add(LogType.info, "There were "+ result.wPoints + " points ("+ Math.Round(numPoints*100, 2) + " %) and " + result.wFaces + " Triangles ("+ Math.Round(numFaces*100, 2) + " %) processed.");
                
            }
            #endregion IFC4 writer (TIN)

            #region logging
            LogWriter.Add(LogType.verbose, "IFC writer completed.");

            //if user didn't select a verbosity level
            if (string.IsNullOrEmpty(jSettings.verbosityLevel.ToString()))
            {
                //set verbosity level to "default" (currently: verbose)
                jSettings.verbosityLevel = LogType.info;

                //logging
                LogWriter.Add(LogType.verbose, "Verbosity level set to " + jSettings.verbosityLevel.ToString());
            }
            
            //log placeholder line
            LogWriter.Add(LogType.info, "--------------------------------------------------");
            #endregion logging
            #endregion writer
            return result;
        }
    }
}
