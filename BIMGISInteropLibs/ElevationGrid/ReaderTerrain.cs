using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//CultureInfo usw.
using System.Globalization;

//BimGisCad - Bibliothek einbinden
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...
using BimGisCad.Representation.Geometry.Composed;   //TIN

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//API to NetTopologySuite
using BIMGISInteropLibs.NTSApi;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//Message box
using System.Windows;

namespace BIMGISInteropLibs.ElevationGrid
{
    class ReaderTerrain
    {
        /// <summary>
        /// Reads out a grid file
        /// </summary>
        /// <param name="is3d">Whether it is a 2D or 3D raster</param>
        /// <param name="fileName">Storage location of the grid</param>
        /// <param name="minDist">minimale Distanz</param>
        /// <param name="size">?[TODO]?</param>
        /// <param name="bBox">Decision to be tailored around grid via a bounding box</param>
        /// <param name="bbNorth">Boundary (North)</param>
        /// <param name="bbEast">Boundary (East)</param>
        /// <param name="bbSouth">Boundary (South)</param>
        /// <param name="bbWest">Boundary (West)</param>
        /// <returns>TIN or MESH for processing in IFCTerrain (and Revit)</returns>
        public static Result ReadGrid(JsonSettings jSettings)
        {
            #region JSON settings
            //Read from json settings
            bool is3d = jSettings.is3D;
            double minDist = jSettings.minDist;
            string fileName = jSettings.filePath;
            int size = jSettings.gridSize;
            bool calculateTin = jSettings.calculateTin;

            //Read bbox from json settings
            bool bBox = jSettings.bBox;
            double bbNorth = jSettings.bbNorth;
            double bbEast = jSettings.bbEast;
            double bbSouth = jSettings.bbSouth;
            double bbWest = jSettings.bbWest;
            #endregion

            //Initialize return variable
            var result = new Result();

            if (calculateTin)
            {
                #region TIN
                if (ReadDtmPointData(fileName, bBox, bbNorth, bbEast, bbSouth, bbWest, out List<double[]> dtmPointList))
                {
                    //Calculate triangulation
                    Tin tin = CalculateTriangulation(dtmPointList);

                    //Pass TIN to result and log
                    result.Tin = tin;
                    AddToLogWriter(LogType.info, "Reading XYZ data successful.");
                    result.rPoints = tin.Points.Count;
                    result.rFaces = tin.NumTriangles;
                    AddToLogWriter(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");
                }
                #endregion
            }
            else
            {
                #region MESH
                //Initialize MESH
                var mesh = new BimGisCad.Collections.Mesh(is3d, minDist); //will be replaced by TIN

                #region Read File for extent
                //Counter for counting the processed lines
                int counter = 0;

                //String for reading out the lines ... Filled only during runtime
                string line;

                //Create lists to fill (replica of the grid file)
                List<double> xList = new List<double>();
                List<double> yList = new List<double>();
                List<double> zList = new List<double>();

                //Create file stream via file location: file can now be read line by line
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);

                //Read line by line and add to lists (x,y,z)
                while ((line = file.ReadLine()) != null)
                {
                    string[] str = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (str.Length > 2
                           && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                           && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                           && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    {
                        xList.Add(x);
                        yList.Add(y);
                        zList.Add(z);
                    }
                    counter++;
                }

                //Write bounding box of input data
                double xMin = xList.Min();
                double xMax = xList.Max();
                double yMin = yList.Min();
                double yMax = yList.Max();

                //Calculate expansion in X-direction and Y-direction
                int xExtent = (int)(xMax - xMin);
                int yExtent = (int)(yMax - yMin);

                file.Close();
                #endregion

                #region Fill Grid
                //Size of the grid
                int xCount = xExtent / size;
                int yCount = yExtent / size;

                //!!!überprüfen ob 0 oder 1 !!!
                var grid = new List<Dictionary<int, Point3>>();
                for (int rowcount = 0; rowcount <= yCount; rowcount++)
                {
                    grid.Add(new Dictionary<int, Point3>());
                }

                //create another file reader to trim points based on bounding box 
                System.IO.StreamReader file2 = new System.IO.StreamReader(fileName);

                while ((line = file2.ReadLine()) != null)
                {
                    //Line is split
                    string[] str = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    //Output the x,y,z values
                    if (str.Length > 2
                           && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                           && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                           && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    {
                        //Decision whether filtering via bounding box or not
                        if (bBox)
                        {
                            //Filter coordinates by min and max values of the bounding box
                            if (y >= bbSouth && y <= bbNorth && x >= bbWest && x <= bbEast)
                            {
                                //Prepare Point Data for BimGisCad
                                Point3 p = Point3.Create(x, y, z);
                                int xGrid = (int)(x - xMin);
                                int yGrid = (int)(y - yMin);
                                grid[yGrid].Add(xGrid, p);
                            }
                        }
                        else
                        {
                            //Prepare Point Data for BimGisCad
                            Point3 p = Point3.Create(x, y, z);
                            int xGrid = (int)(x - xMin);
                            int yGrid = (int)(y - yMin);
                            grid[yGrid].Add(xGrid, p);
                        }
                    }
                }
                //File-Reader #2 - close
                file2.Close();
                #endregion

                //here the meshing is created
                #region Create simple Mesh on Grid
                for (int row = 0; row < yCount; row++)
                {
                    for (int column = 0; column <= xCount; column++)
                    {
                        if (grid[row].ContainsKey(column))
                        {
                            Point3 cp = grid[row][column];
                            mesh.AddPoint(cp);

                            //Triangle1 TopLeft
                            if (grid[row + 1].ContainsKey(column + 1) && grid[row + 1].ContainsKey(column))
                            {
                                Point3 tp = grid[row + 1][column];
                                Point3 tr = grid[row + 1][column + 1];

                                mesh.AddFace(new[] { cp, tp, tr });
                            }

                            //Triangle2 BottomRight
                            if (grid[row + 1].ContainsKey(column + 1) && grid[row].ContainsKey(column + 1))
                            {
                                Point3 br = grid[row][column + 1];
                                Point3 tr = grid[row + 1][column + 1];

                                mesh.AddFace(new[] { cp, tr, br });
                            }

                            //Triangle on left Edge of Terrain
                            if (grid[row].ContainsKey(column - 1) is false && grid[row + 1].ContainsKey(column) && grid[row + 1].ContainsKey(column - 1))
                            {
                                Point3 tp = grid[row + 1][column];
                                Point3 tl = grid[row + 1][column - 1];

                                mesh.AddFace(new[] { cp, tl, tp });
                            }
                        }
                    }
                }

                //Pass MESH to result
                result.Mesh = mesh;
                result.rPoints = mesh.Points.Count;
                result.rFaces = mesh.FaceEdges.Count;
                #endregion
                #endregion
            }

            //Return the result as TIN or MESH
            return result;
        }

        /// <summary>
        /// A function to create a TIN using BimGisCad TIN builder and an interface to NetTopologySuite class library to calculate triangles.
        /// </summary>
        /// <param name="dtmPointList">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <returns>A TIN structured by BimGisCad TIN builder</returns>
        public static Tin CalculateTriangulation(List<double[]> dtmPointList)
        {
            //Initialize TIN builder
            var tinBuilder = Tin.CreateBuilder(true);

            //Log TIN builder initalization
            AddToLogWriter(LogType.verbose, "[XYZ] Initialize a TIN builder.");

            //Get a list of triangles via NetTopologySuite class library using the interface object
            List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointList);
            
            //Read out each triangle from the triangle list
            int pnr = 0;
            foreach (List<double[]> dtmTriangle in dtmTriangleList)
            {
                //Read out the three vertices of one triangle at each loop
                Point3 p1 = Point3.Create(Math.Round(dtmTriangle[0][0], 3), Math.Round(dtmTriangle[0][1], 3), Math.Round(dtmTriangle[0][2], 3));
                Point3 p2 = Point3.Create(Math.Round(dtmTriangle[1][0], 3), Math.Round(dtmTriangle[1][1], 3), Math.Round(dtmTriangle[1][2], 3));
                Point3 p3 = Point3.Create(Math.Round(dtmTriangle[2][0], 3), Math.Round(dtmTriangle[2][1], 3), Math.Round(dtmTriangle[2][2], 3));

                //Add the triangle vertices to the TIN builder and log point coordinates
                tinBuilder.AddPoint(pnr++, p1);
                AddToLogWriter(LogType.verbose, "[XYZ] Point set (x= " + p1.X + "; y= " + p1.Y + "; z= " + p1.Z + ")");
                tinBuilder.AddPoint(pnr++, p2);
                AddToLogWriter(LogType.verbose, "[XYZ] Point set (x= " + p2.X + "; y= " + p2.Y + "; z= " + p2.Z + ")");
                tinBuilder.AddPoint(pnr++, p3);
                AddToLogWriter(LogType.verbose, "[XYZ] Point set (x= " + p3.X + "; y= " + p3.Y + "; z= " + p3.Z + ")");

                //Add the index of each vertex to the TIN builder (defines triangle) and log
                for (int i = pnr - 3; i < pnr; i++)
                {
                    tinBuilder.AddTriangle(i++, i++, i++);
                    AddToLogWriter(LogType.verbose, "[XYZ] Triangle set.");
                }
            }

            //Build and return a TIN via BimGisCad class library and log
            Tin tin = tinBuilder.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            AddToLogWriter(LogType.verbose, "[XYZ] Creating TIN via TIN builder.");
            return tin;
        }

        /// <summary>
        /// A auxiliary function to read the input file and create a list of DTM point data.
        /// </summary>
        /// <param name="fileName">The file path.</param>
        /// <param name="bBox">A boolean value if a bounding box is used or not.</param>
        /// <param name="bbSouth">The southern Y-coordinate of the bounding box.</param>
        /// <param name="bbNorth">The northern Y-coordinate of the bounding box.</param>
        /// <param name="bbWest">The western Y-coordinate of the bounding box.</param>
        /// <param name="bbEast">The eastern Y-coordinate of the bounding box.</param>
        /// <returns>A List of DTM point data</returns>
        public static bool ReadDtmPointData(string fileName, bool bBox, double bbSouth, double bbNorth, double bbWest, double bbEast, out List<double[]> dtmPointList)
        {
            //Initialize list for DTM point data
            dtmPointList = new List<double[]>();

            try
            {
                //A single line of the input file
                string line;

                //Create a file reader to trim points based on bounding box 
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);

                //Read out each line of the input file
                while ((line = file.ReadLine()) != null)
                {
                    //Line is split
                    string[] str = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    //Output the x,y,z values
                    if (str.Length > 2
                           && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                           && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                           && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    {
                        //Decision whether filtering via bounding box or not
                        if (bBox)
                        {
                            //Filter coordinates by min and max values of the bounding box
                            if (y >= bbSouth && y <= bbNorth && x >= bbWest && x <= bbEast)
                            {
                                //Prepare Point Data for NetTopologySuite
                                dtmPointList.Add(new double[] { x, y, z });
                            }
                        }
                        else
                        {
                            //Prepare Point Data for NetTopologySuite
                            dtmPointList.Add(new double[] { x, y, z });
                        }
                    }
                }

                //Close file reader
                file.Close();

                //Log successful reading
                AddToLogWriter(LogType.verbose, "XYZ file has been read (" + fileName + ")");

                //Return true in case reading the input file was successful
                return true;
            }
            catch (Exception e)
            {
                //Log failed reading
                AddToLogWriter(LogType.error, "XYZ file could not be read (" + fileName + ")");

                //Show meassage box with exception
                MessageBox.Show("XYZ file could not be read: \n" + e.Message, "XYZ file reader", MessageBoxButton.OK, MessageBoxImage.Error);

                //Return false in case reading the input file failed
                return false;
            }
        }

        /// <summary>
        /// A auxiliary function to feed the log writer.
        /// </summary>
        /// <param name="logType">The type og logging.</param>
        /// <param name="message">The message to log.</param>
        public static void AddToLogWriter(LogType logType, string message)
        {
            LogWriter.Entries.Add(new LogPair(logType, message));
        }
    }
}
