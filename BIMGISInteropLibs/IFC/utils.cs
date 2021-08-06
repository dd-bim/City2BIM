using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc; //IfcStore
using Xbim.IO;  //storage type

//embed for Logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using BIMGISInteropLibs.IfcTerrain;

using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry;

namespace BIMGISInteropLibs.IFC
{
    /// <summary>
    /// collection for small functions in context with IFC processing
    /// </summary>
    public class utils
    {
        #region rotation
        /// <summary>
        /// rotation vector
        /// </summary>
        public static double[] getRotationVector(double azi)
        {
            double[] vector = AzimuthToVector(azi);
            return new[] { vector[0], vector[1] };
        }
        /// <summary>
        /// support function to calclue azimuth to vector
        /// </summary>
        private static double[] AzimuthToVector(double azi)
        {
            var razi = DegToRad(azi);
            return new[] { Math.Cos(razi), Math.Sin(razi) };
        }
        /// <summary>
        /// support to calc rho
        /// </summary>
        private static readonly double RevRho = Math.PI / 180.0;

        /// <summary>
        /// calc deg to rad
        /// </summary>
        private static double DegToRad(double deg) => deg * RevRho;
        #endregion rotation

        #region meridian conv
        #endregion meridian conv

        #region write input
        /// <summary>
        /// setting write input (may update from BIMGISCAD lib to "new internal" lib
        /// </summary>
        public static WriteInput setWriteInput(Result result, Config config)
        {
            #region placement / georef
            //Placment / Georeferencing
            var writeInput = new WriteInput();
            writeInput.Placement = Axis2Placement3D.Standard;

            //init new double values
            double originX;
            double originY;
            double originZ;

            //query whether a user has assigned "coordinates"
            if (config.customOrigin.Value)
            {
                originX = config.xOrigin.Value;
                originY = config.yOrigin.Value;
                originZ = config.zOrigin.Value;

                LogWriter.Add(LogType.info, "Orgin via user input has been set.");
            }
            else
            {
                originX = result.geomStore.Centroid.X;
                originY = result.geomStore.Centroid.Y;
                originZ = 0;
                LogWriter.Add(LogType.info, "Orgin (XY) cacluate from data.");
            }
            //set center point as placement
            writeInput.Placement.Location = Vector3.Create(originX, originY, originZ);
            LogWriter.Add(LogType.debug, "Project center: X= " + originX + "; Y= " + originY + "; Z= " + originZ);
            writeInput.Placement.RefDirection = Direction3.Create(utils.getRotationVector(config.trueNorth.GetValueOrDefault())[0], utils.getRotationVector(config.trueNorth.GetValueOrDefault())[1], 0, null);
            #endregion placement / georef

            #region shape representation
            //interface for IFC shape representation on the basis of which the Shape - Representation are controlled
            writeInput.SurfaceType = SurfaceType.TFS; //standard case
            //Query whether another shape representation is desired
            if (config.surfaceType == SurfaceType.GCS)
            {
                //set shape repr. to GCS
                writeInput.SurfaceType = SurfaceType.GCS;
            }
            else if (config.surfaceType == SurfaceType.SBSM)
            {
                //set shape repr. to SBSM
                writeInput.SurfaceType = SurfaceType.SBSM;
            }
            else if (config.surfaceType == SurfaceType.TIN)
            {
                writeInput.SurfaceType = SurfaceType.TIN;
            }

            //logging
            LogWriter.Add(LogType.verbose, "Writer is using shape representation: " + writeInput.SurfaceType.ToString());
            #endregion shape representation

            #region ifc xml / step file
            //query whether an XML file is to be written additionally (IFC4 only)
            if (config.outFileType == IfcFileType.ifcXML)
            {
                writeInput.FileType = IfcFileType.ifcXML;
                LogWriter.Add(LogType.debug, "Using settings to output ifcXML");
            }
            else if (config.outFileType == IfcFileType.ifcZip)
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


            return writeInput;
        }
        #endregion write input


        #region ifc file writer
        /// <summary>
        /// this method write the dest file <para/>
        /// supports STEP, XML, ZIP
        /// </summary>
        public static void WriteFile(IfcStore model, Config config)
        {
            switch (config.outFileType)
            {
                //if it is to be saved as an STEP file
                case IfcFileType.Step:
                    try
                    {
                        model.SaveAs(config.destFileName, StorageType.Ifc);
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + config.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + config.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;

                //if it is to be saved as an XML file
                case IfcFileType.ifcXML:
                    try
                    {
                        model.SaveAs(config.destFileName, StorageType.IfcXml);
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + config.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + config.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;

                //if it is to be saved as an ifcZIP file
                case IfcFileType.ifcZip:
                    try
                    {
                        model.SaveAs(config.destFileName, StorageType.IfcZip);
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + config.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + config.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;
            }
        }
        #endregion
    }
}
