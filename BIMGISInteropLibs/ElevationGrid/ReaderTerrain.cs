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
        /// <returns>TIN for processing in IFCTerrain (and Revit)</returns>
        public static Result ReadGrid(JsonSettings jSettings)
        {
            //read from json settings
            bool is3d = jSettings.is3D;
            double minDist = jSettings.minDist;
            string fileName = jSettings.filePath;
            int size = jSettings.gridSize;
            
            //Return variable initalize
            var result = new Result();

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

            //Create dictionary to process TIN [TODO]
            var tinGrid = new List<Dictionary<int, int>>();
            //GRID fill ...

            //create another file reader to trim points based on BB box 
            System.IO.StreamReader file2 = new System.IO.StreamReader(fileName);

            //read from json settings
            bool bBox = jSettings.bBox;

            double bbNorth = jSettings.bbNorth;
            double bbEast = jSettings.bbEast;
            double bbSouth = jSettings.bbSouth;
            double bbWest = jSettings.bbWest;

            
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
                        if (y >= bbSouth && y <= bbNorth && x >= bbWest && x <= bbEast)
                        {
                            Point3 p = Point3.Create(x, y, z);

                            int xGrid = (int)(x - xMin);
                            int yGrid = (int)(y - yMin);

                            grid[yGrid].Add(xGrid, p);
                            //Add points to the TIN builder
                            //Indexing by "artificial" point number
                            //EMPTY
                        }
                    }
                    else
                    {
                        //Create point via BimGisCad - Bib.
                        Point3 p = Point3.Create(x, y, z);
                        
                        int xGrid = (int)(x - xMin);
                        int yGrid = (int)(y - yMin);

                        grid[yGrid].Add(xGrid, p);

                        //Add point to TIN via PNR
                        //tinB.AddPoint(pnr, p);

                        //Fill TIN grid & increment the point number, because with the next line the next point follows
                        //tinGrid[pnr++].Add(yGrid, xGrid);
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

                            //tinB.AddTriangle();

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
            #endregion

            //Return of a MESH [TODO]: Revise so that a TIN is returned.
            result.Mesh = mesh;

            result.rPoints = mesh.Points.Count;
            result.rFaces = mesh.FaceEdges.Count;
            return result;
        }
    }
}
