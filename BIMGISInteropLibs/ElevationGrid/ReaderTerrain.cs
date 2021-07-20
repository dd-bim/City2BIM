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

//shortcut for tin building class
using terrain = BIMGISInteropLibs.Geometry.terrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//Message box
using System.Windows;

//Compute triangulation
using BIMGISInteropLibs.Triangulator;

namespace BIMGISInteropLibs.ElevationGrid
{
    class ReaderTerrain
    {
        /// <summary>
        /// Reads out a grid file
        /// </summary>
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
                    Tin tin = IfcTerrainTriangulator.CreateTin(dtmPointList);

                    //Pass TIN to result and log
                    result.Tin = tin;
                    LogWriter.Add(LogType.info, "Reading XYZ data successful.");
                    result.rPoints = tin.Points.Count;
                    result.rFaces = tin.NumTriangles;
                    LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");
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
                LogWriter.Add(LogType.verbose, "XYZ file has been read (" + fileName + ")");

                //Return true in case reading the input file was successful
                return true;
            }
            catch (Exception e)
            {
                //Log failed reading
                LogWriter.Add(LogType.error, "XYZ file could not be read (" + fileName + ")");

                //write to console
                Console.WriteLine("XYZ file could not be read: " + Environment.NewLine + e.Message);

                //Return false in case reading the input file failed
                return false;
            }
        }
    }
}
