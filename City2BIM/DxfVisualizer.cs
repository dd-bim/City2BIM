using netDxf;

namespace City2BIM
{
    public class DxfVisualizer
    {
        private DxfDocument dxf = new DxfDocument();

        public DxfVisualizer()
        {
            this.dxf = new DxfDocument();
        }

        /// <summary>
        ///  Create Point for DXF
        /// </summary>
        public void DrawPoint(double x, double y, double z, string layername, int[] color)
        {
            var pt = new netDxf.Entities.Point(x, y, z);
            pt.Layer = SetLayer(layername, color);
            dxf.AddEntity(pt);

            var circle = new netDxf.Entities.Circle(new Vector3(x, y, z), 1);
            circle.Layer = SetLayer(layername, color);
            dxf.AddEntity(circle);
        }

        /// <summary>
        ///  Create Segement for DXF
        ///  xyz of two Points needed
        /// </summary>
        public void DrawLine(double x, double y, double z, double x2, double y2, double z2, string layername, int[] color)
        {
            var lineDxf = new netDxf.Entities.Line(new Vector3(x, y, z), new Vector3(x2, y2, z2));
            lineDxf.Layer = SetLayer(layername, color);

            dxf.AddEntity(lineDxf);
        }

        private netDxf.Tables.Layer SetLayer(string name, int[] color)
        {
            var layer = new netDxf.Tables.Layer(name);
            layer.Color = new AciColor(color[0], color[1], color[2]);

            return layer;
        }

        /// <summary>
        ///  DXF-Export
        /// </summary>
        public void DrawDxf(string path)
        {
            this.dxf.Save(path + ".dxf");
            //System.Diagnostics.Process.Start(path + ".dxf");
        }
    }
}