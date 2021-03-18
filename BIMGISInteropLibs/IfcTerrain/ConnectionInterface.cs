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

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

using BIMGISInteropLibs.IFC;

using BIMGISInteropLibs.Logging; //logging
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
//IxMilia: for processing dxf files
using IxMilia.Dxf;

using System.Windows; //include for error message box

namespace BIMGISInteropLibs.IfcTerrain
{
    public class ConnectionInterface
    {
        //initialize TIN / MESH / Breaklines
        /// <summary>
        /// MESH (result of the specific file reader [TODO] remove, when no longer needed)
        /// </summary>
        public Mesh Mesh { get; private set; } = null;

        /// <summary>
        /// TIN (result of the specific file reader)
        /// </summary>
        public Tin Tin { get; private set; }

        /// <summary>
        /// breaklines (result of the specific file reader) [TODO]
        /// </summary>
        public Dictionary<int, Line3> Breaklines { get; private set; } = null;
      
        /// <summary>
        /// ConnectionInterface between file reader and ifc writer
        /// </summary>
        /// <param name="jSettings">JsonSettings read from Command or GUI</param>
        /// <param name="breakDist">TODO</param>
        /// <param name="refLatitude">TODO</param>
        /// <param name="refLongitude">TODO</param>
        /// <param name="refElevation">TODO</param>
        public void mapProcess(JsonSettings jSettings, double? breakDist = null, double? refLatitude = null, double? refLongitude = null, double? refElevation = null)
        {
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Mapping process started."));

            //The processing is basically done by a reader and a writer (these are included in the corresponding regions)
            #region reader
            //initalize transfer class
            var result = new Result();

            //File location of the file to be converted 
            //[TODO]: check if: string filePath = jSettings.filePath; would work! 
            string[] filePath = new string[1];
            filePath[0] = jSettings.filePath;

            #region import data via type selection
            //In the following a mapping is made on the basis of the data type, so that the respective reader is called up
            switch (jSettings.fileType)
            {
                //reader for LandXML
                case IfcTerrainFileType.LandXML:
                    //result = LandXML.ReaderTerrain(jSettings.is3D, jSettings.filePath, jSettings.minDist, jSettings.logFilePath, jSettings.verbosityLevel);
                    result = LandXML.ReaderTerrain.ReadTin(jSettings.filePath);
                    //end of the case so it is jumped after file reading (above) out of the switch function
                    break;

                //reader for CityGML
                case IfcTerrainFileType.CityGML:
                    result = CityGML.CityGMLReaderTerrain.ReadTin(jSettings.filePath);
                    break;

                //reader for DXF
                case IfcTerrainFileType.DXF:
                    //file Reader: output will be used to process terrain information
                    DXF.ReaderTerrain.ReadFile(jSettings.filePath, out DxfFile dxfFile);

                    //loop for distinguishing whether it is a tin or not (processing via points and lines)
                    if (jSettings.isTin)
                    {
                        //Tin - Reader (if dxf file contains faces)
                        result = DXF.ReaderTerrain.ReadDxfTin(dxfFile, jSettings.layer, jSettings.breakline_layer, jSettings.minDist, jSettings.breakline);
                    }
                    else
                    {
                        //TODO: change to tin processing
                        //processing points and lines (via mesh)
                        result = DXF.ReaderTerrain.ReadDxfPoly(jSettings.is3D, dxfFile, jSettings.layer, jSettings.breakline_layer, jSettings.minDist, jSettings.logFilePath, jSettings.breakline);
                    }
                    break;

                //reader for REB    
                case IfcTerrainFileType.REB:
                    //REB file reader
                    REB.RebDaData rebData = REB.ReaderTerrain.ReadReb(filePath);
                    //use REB data via processing with converter
                    result = REB.ReaderTerrain.ConvertRebToTin(rebData, jSettings.horizon);
                    break;

                //reader for Elev.Grid [TODO]: processing result is mesh! have to be changed to tin
                case IfcTerrainFileType.Grid:
                    result = ElevationGrid.ReaderTerrain.ReadGrid(jSettings.is3D, jSettings.filePath, jSettings.minDist, jSettings.gridSize, jSettings.bBox, jSettings.bbNorth, jSettings.bbEast, jSettings.bbSouth, jSettings.bbWest);
                    break;

                //reader for GRAFBAT
                case IfcTerrainFileType.Grafbat:
                    if (jSettings.isTin)
                    {
                        //reader for processing GRAFBAT as TIN via points and triangles 
                        result = GEOgraf.ReadOUT.ReadOutData(jSettings.filePath, out IReadOnlyDictionary<int, int> pointIndex2NumberMap, out IReadOnlyDictionary<int, int> triangleIndex2NumerMap);
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
                    result = PostGIS.ReaderTerrain.ReadPostGIS(jSettings.host, jSettings.port, jSettings.user, jSettings.password, jSettings.database, jSettings.schema, jSettings.tin_table, jSettings.tin_column, jSettings.tinid_column, jSettings.tin_id, jSettings.breakline, jSettings.breakline_table, jSettings.breakline_column, jSettings.breakline_tin_id, jSettings);
                    break;
            }
            //so that from the reader (TIN, Error, Mesh) is passed to respective "classes"
            LogWriter.Entries.Add(new LogPair(LogType.debug, "Reading file completed."));

            this.Tin = result.Tin;      //pass the TIN

            //tin error handler
            if(this.Tin.NumTriangles.Equals(0))
            {
                //log error message
                LogWriter.Entries.Add(new LogPair(LogType.error, "[READER]no DTM could be read on the basis of the input data. The DTM is empty! - Processing canceled!"));

                //write log file
                LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));
                
                //Messagebox
                MessageBox.Show("The TIN is empty! - Processing canceled!", "TIN is empty", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
            else
            {
                //log reading results
                LogWriter.Entries.Add(new LogPair(LogType.info, "TIN read: " + Tin.Points.Count + " points; " + Tin.NumTriangles + " triangles;"));
            }

            this.Mesh = result.Mesh;    //pass the MESH (will be removed) [TODO]
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
                LogWriter.Entries.Add(new LogPair(LogType.warning, "Project name have been set as placeholder."));
            }

            #region placement / georef
            //Placment / Georeferencing
            var writeInput = new WriteInput();
            writeInput.Placement = Axis2Placement3D.Standard;

            //query whether a user has assigned "coordinates"
            if (jSettings.customOrigin)
            {
                //Placement is set via input values
                writeInput.Placement.Location = Vector3.Create(jSettings.xOrigin, jSettings.yOrigin, jSettings.zOrigin);
                LogWriter.Entries.Add(new LogPair(LogType.debug, "Using origin: X= "+ jSettings.xOrigin + "; Y= "+jSettings.yOrigin + "; Z= "+ jSettings.zOrigin));
            }
            //Alternative: determine the "BoundingBox", and define the center (x,y,z) as the coordinate origin.
            else
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
                    double midX = (this.Mesh.MaxX + this.Mesh.MinX) / 2;
                    double midY = (this.Mesh.MaxY + this.Mesh.MinY) / 2;
                    double midZ = (this.Mesh.MaxZ + this.Mesh.MinZ) / 2;

                    //set center point as placement
                    writeInput.Placement.Location = Vector3.Create(midX, midY, midZ);
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
                    double MidX = (MaxX + MinX) / 2;
                    double MidY = (MaxY + MinY) / 2;
                    double MidZ = (MaxZ + MinZ) / 2;

                    //set center point as placement
                    writeInput.Placement.Location = Vector3.Create(MidX, MidY, MidZ);
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "Project center point was calculated and set."));
                    LogWriter.Entries.Add(new LogPair(LogType.debug, "Project center: X= " + MidX + "; Y= " + MidY + "; Z= " + MidZ));
                }
            }
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
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Writer is using shape representation: " + writeInput.SurfaceType.ToString()));
            #endregion shape representation

            #region ifc xml / step file
            //Standard file type to STEP (Reason: supported by IFC2x3 *AND* IFC4, etc.)
            writeInput.FileType = IfcFileType.Step;

            //query whether an XML file is to be written additionally (IFC4 only)
            if (jSettings.outFileType == IfcFileType.XML)
            {
                writeInput.FileType = IfcFileType.XML;
                LogWriter.Entries.Add(new LogPair(LogType.debug, "Using settings to output IfcXml"));
            }
            #endregion ifc xml / step file

            //Logging
            LogWriter.Entries.Add(new LogPair(LogType.debug, "Writing IFC with:"));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> IFC Version " + jSettings.outIFCType));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> Surfacetype " + jSettings.surfaceType));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> Filetype " + jSettings.fileType));

            //(processing via MESH [TODO]: is to be replaced by TIN - writer (below))
            #region IFC2x3 writer (MESH)
            //Query whether output IFC file should be version IFC2x3
            if (jSettings.outIFCType == IfcVersion.IFC2x3)
            {
                //model (file) let generate via the Writer
                var model = IFC.Ifc2x3.Store.CreateViaMesh(
                    jSettings.projectName,
                    jSettings.editorsFamilyName,
                    jSettings.editorsGivenName,
                    jSettings.editorsOrganisationName,
                    "Site with Terrain", //TODO: check, if this is required (or whether it can be created automatically)
                    writeInput.Placement,
                    this.Mesh,
                    writeInput.SurfaceType,
                    breakDist);

                //TODO LOGGING
                //logger.Debug("IFC site created");

                //access to file writer 
                IFC.Ifc2x3.Store.WriteFile(model, jSettings.destFileName, writeInput.FileType == IfcFileType.XML);

                //logging - ifc file is written to path ...
            }
            #endregion IFC2x3 writer (MESH)

            #region IFC4 writer (MESH)
            else if (jSettings.outIFCType == IfcVersion.IFC4)
            {
                //logger.Debug("Geographical Element: " + jSettings.geoElement); [TODO]:Logging

                //create IFC4 modell
                var model = jSettings.geoElement
                    //create with geo element
                    ? IFC.Ifc4.Geo.CreateViaMesh(
                        jSettings.projectName,
                        jSettings.editorsFamilyName,
                        jSettings.editorsGivenName,
                        jSettings.editorsOrganisationName,
                        "Site with Terrain",
                        writeInput.Placement,
                        this.Mesh,
                        writeInput.SurfaceType,
                        breakDist)
                    //with create with geo element
                    : IFC.Ifc4.Store.CreateViaMesh(
                        jSettings.projectName,
                        jSettings.editorsFamilyName,
                        jSettings.editorsGivenName,
                        jSettings.editorsOrganisationName,
                        "Site with Terrain",
                        writeInput.Placement,
                        this.Mesh,
                        writeInput.SurfaceType,
                        breakDist);
                //logger.Debug("IFC Site created"); [TODO]:Logging
                IFC.Ifc4.Store.WriteFile(model, jSettings.destFileName, writeInput.FileType == IfcFileType.XML);
                //logger.Info("IFC file writen: " + jSettings.destFileName);
            }
            #endregion IFC4 writer (MESH)

            #region IFC2x3 writer (TIN)
            //Query whether output IFC file should be version IFC2x3
            else if (jSettings.outIFCType == IfcVersion.IFC2x3Tin)
            {
                //model (file) let generate via the Writer
                var model = IFC.Ifc2x3.Store.CreateViaTin(
                    jSettings.projectName,
                    jSettings.editorsFamilyName,
                    jSettings.editorsGivenName,
                    jSettings.editorsOrganisationName,
                    "Site with Terrain", //TODO: check, if this is required (or whether it can be created automatically)
                    writeInput.Placement,
                    this.Tin,
                    writeInput.SurfaceType,
                    breakDist);

                //TODO LOGGING
                //logger.Debug("IFC site created");

                //access to file writer 
                IFC.Ifc2x3.Store.WriteFile(model, jSettings.destFileName, writeInput.FileType == IfcFileType.XML);

                //logging - ifc file is written to path ...
            }
            #endregion IFC2x3 writer (TIN)

            #region IFC4 writer (TIN)
            else if (jSettings.outIFCType == IfcVersion.IFC4Tin)
            {
                //logger.Debug("Geographical Element: " + jSettings.geoElement); [TODO]:Logging

                //create IFC4 modell
                var model = jSettings.geoElement
                    //create with geo element
                    ? IFC.Ifc4.Geo.CreateViaTin(
                        jSettings.projectName,
                        jSettings.editorsFamilyName,
                        jSettings.editorsGivenName,
                        jSettings.editorsOrganisationName,
                        "Site with Terrain",
                        writeInput.Placement,
                        this.Tin,
                        this.Breaklines, //TODO: ATTETION DRAFT Version of Breaklines
                        writeInput.SurfaceType,
                        breakDist)
                    //with create with geo element
                    : IFC.Ifc4.Store.CreateViaTin(
                        jSettings.projectName,
                        jSettings.editorsFamilyName,
                        jSettings.editorsGivenName,
                        jSettings.editorsOrganisationName,
                        "Site with Terrain",
                        writeInput.Placement,
                        this.Tin,
                        writeInput.SurfaceType,
                        breakDist);
                //logger.Debug("IFC Site created"); [TODO]:Logging
                IFC.Ifc4.Store.WriteFile(model, jSettings.destFileName, writeInput.FileType == IfcFileType.XML);
                LogWriter.Entries.Add(new LogPair(LogType.info, "IFC file writen: " + jSettings.destFileName));
                //logger.Info("IFC file writen: " + jSettings.destFileName);
            }
            #endregion IFC4 writer (TIN)



            #region logging
            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "IFC writer completed."));

            //if user didn't select a verbosity level
            if (string.IsNullOrEmpty(jSettings.verbosityLevel.ToString()))
            {
                //set verbosity level to "default" (currently: verbose)
                jSettings.verbosityLevel = LogType.info;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Verbosity level set to " + jSettings.verbosityLevel.ToString()));
            }

            LogWriter.Entries.Add(new LogPair(LogType.info, "--------------------------------------------------"));

            //write to logging file
            LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));
            #endregion logging
            #endregion writer
        }
    }
}
