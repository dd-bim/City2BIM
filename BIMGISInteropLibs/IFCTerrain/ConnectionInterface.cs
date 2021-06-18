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

using System.Windows; //include for output error messagebox

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
        public Result mapProcess(JsonSettings jSettings, JsonSettings_DIN_SPEC_91391_2 jSettings_DIN91931, JsonSettings_DIN_18740_6 jSettings_DIN18740, double? breakDist = null, double? refLatitude = null, double? refLongitude = null, double? refElevation = null)
        {
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Mapping process started."));

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

                    //loop for distinguishing whether it is a tin or not (processing via points and lines)
                    if (jSettings.isTin)
                    {
                        //Tin - Reader (if dxf file contains faces)
                        result = DXF.ReaderTerrain.ReadDxfTin(dxfFile, jSettings);
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

                    //use REB data via processing with converter
                    result = REB.ReaderTerrain.ConvertRebToTin(rebData, jSettings);
                    break;

                //reader for Elev.Grid: processing result is mesh!
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
                    result = PostGIS.ReaderTerrain.ReadPostGIS(jSettings);
                    break;
            }
            //so that from the reader (TIN, Error, Mesh) is passed to respective "classes"
            LogWriter.Entries.Add(new LogPair(LogType.debug, "Reading file completed."));

            //passing results from reader to local var 
            this.Tin = result.Tin;      //passing tin
            this.Mesh = result.Mesh;    //passing mesh

            //tin error handler
            if (jSettings.isTin)
            {
                if (this.Tin.NumTriangles.Equals(0))
                {
                    //log error message
                    LogWriter.Entries.Add(new LogPair(LogType.error, "[READER] no DTM could be read on the basis of the input data. The DTM is empty! - Processing canceled!"));

                    //write log file
                    LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));

                    //Messagebox
                    MessageBox.Show("The TIN is empty! - Processing canceled!", "TIN is empty", MessageBoxButton.OK, MessageBoxImage.Error);

                    return result;
                }
            }

            //mesh error handler
            else if (!jSettings.isTin)
            {
                //log reading results
                LogWriter.Entries.Add(new LogPair(LogType.info, "MESH read: " + Mesh.Points.Count + " points; " + Mesh.FaceEdges.Count + " triangles;"));
            }
            //if tin has been readed succesfully
            else
            {
                //log reading results
                LogWriter.Entries.Add(new LogPair(LogType.info, "TIN read: " + Tin.Points.Count + " points; " + Tin.NumTriangles + " triangles;"));
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

                    //logging
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "Project center point was calculated and set."));
                    LogWriter.Entries.Add(new LogPair(LogType.debug, "Project center: X= " + MidX + "; Y= " + MidY + "; Z= " + MidZ));
                }
            }
            //case mesh (not needed to calculate min or max values)
            else if(!jSettings.isTin)
            {
                //get center point via readed mesh
                double MidX = (this.Mesh.MinX + this.Mesh.MaxX) / 2;
                double MidY = (this.Mesh.MinY + this.Mesh.MaxY) / 2;
                double MidZ = (this.Mesh.MinZ + this.Mesh.MaxZ) / 2;

                //set center point as placement
                writeInput.Placement.Location = Vector3.Create(MidX, MidY, MidZ);
                
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Project center point was calculated and set."));
                LogWriter.Entries.Add(new LogPair(LogType.debug, "Project center: X= " + MidX + "; Y= " + MidY + "; Z= " + MidZ));
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
            

            //query whether an XML file is to be written additionally (IFC4 only)
            if (jSettings.outFileType == IfcFileType.ifcXML)
            {
                writeInput.FileType = IfcFileType.ifcXML;
                LogWriter.Entries.Add(new LogPair(LogType.debug, "Using settings to output ifcXML"));
            }
            else if (jSettings.outFileType == IfcFileType.ifcZip)
            {
                writeInput.FileType = IfcFileType.ifcZip;
                LogWriter.Entries.Add(new LogPair(LogType.debug, "Using settings to output ifcZIP"));
            }
            else
            {
                //Standard file type to STEP (Reason: supported by IFC2x3 *AND* IFC4, etc.)
                writeInput.FileType = IfcFileType.Step;

                LogWriter.Entries.Add(new LogPair(LogType.debug, "Using settings to output STEP"));
            }
            #endregion ifc xml / step file

            //Logging
            LogWriter.Entries.Add(new LogPair(LogType.debug, "Writing IFC with:"));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> IFC Version " + jSettings.outIFCType));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> Surfacetype " + jSettings.surfaceType));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "--> Filetype " + jSettings.fileType));

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
                LogWriter.Entries.Add(new LogPair(LogType.info, "IFC file writen: " + jSettings.destFileName));

                //logging stat
                double numPoints = (double)result.wPoints / (double)result.rPoints;
                double numFaces = (double)result.wFaces / (double)result.rFaces;
                LogWriter.Entries.Add(new LogPair(LogType.info, "There were " + result.wPoints + " points (" + Math.Round(numPoints * 100, 2) + " %) and " + result.wFaces + " Triangles (" + Math.Round(numFaces * 100, 2) + " %) processed."));

            }
            #endregion IFC2x3 writer (TIN)

            #region IFC4 writer (TIN)
            else if (jSettings.outIFCType == IfcVersion.IFC4)
            {
                //create IFC4 modell
                var model = jSettings.geoElement
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
                LogWriter.Entries.Add(new LogPair(LogType.info, "IFC file writen: " + jSettings.destFileName));

                //logging stat
                double numPoints = (double)result.wPoints / (double)result.rPoints;
                double numFaces = (double)result.wFaces / (double)result.rFaces;
                LogWriter.Entries.Add(new LogPair(LogType.info, "There were "+ result.wPoints + " points ("+ Math.Round(numPoints*100, 2) + " %) and " + result.wFaces + " Triangles ("+ Math.Round(numFaces*100, 2) + " %) processed."));
                
            }
            #endregion IFC4 writer (TIN)

            #region logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "IFC writer completed."));

            //if user didn't select a verbosity level
            if (string.IsNullOrEmpty(jSettings.verbosityLevel.ToString()))
            {
                //set verbosity level to "default" (currently: verbose)
                jSettings.verbosityLevel = LogType.info;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Verbosity level set to " + jSettings.verbosityLevel.ToString()));
            }
            
            //log placeholder line
            LogWriter.Entries.Add(new LogPair(LogType.info, "--------------------------------------------------"));

            //write to log file
            LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));
            #endregion logging
            #endregion writer

            return result;
        }
    }
}
