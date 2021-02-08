using System.Windows.Forms;

namespace City2RVT.Reader
{
    internal class FileDialog
    {
        public enum Data { CityGML, ALKIS, DGM, XPlanGML, IFC, JSON, GMLXML };

        public string ImportPath(Data geodata)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            switch (geodata)
            {
                case (Data.CityGML):
                    {
                        ofd.Title = "Select CityGML file.";
                        ofd.Filter = "CityGML (*.gml) | *.gml| CityGML (*.xml) | *.xml| All Files (*.*) | *.*";
                        break;
                    }
                case (Data.ALKIS):
                    {
                        ofd.Title = "Select NAS-ALKIS file.";
                        ofd.Filter = "NAS-XML |*.xml| All Files (*.*) | *.*";
                        break;
                    }
                case (Data.DGM):
                    {
                        ofd.Title = "Select Grid terrain file.";
                        ofd.Filter = "DTM - XYZ coordinates (*.txt, *.xyz, *.csv)|*.txt; *.xyz; *.csv|All files (*.*)|*.*";
                        break;
                    }
                case (Data.XPlanGML):
                    {
                        ofd.Title = "Select XPlanung file.";
                        ofd.Filter = "XPlanGML (*.gml) | *.gml| All Files (*.*) | *.*";
                        break;
                    }
                case (Data.GMLXML):
                    {
                        ofd.Title = "Select Reset file.";
                        ofd.Filter = "XPlanGML (*.gml) | *.gml|NAS - XML | *.xml| All Files (*.*) | *.*";
                        break;
                    }
                case (Data.IFC):
                    {
                        ofd.Title = "Select IFC file.";
                        ofd.Filter = "IFC (*.ifc) | *.ifc| All Files (*.*) | *.*";
                        break;
                    }
                case (Data.JSON):
                    {
                        ofd.Title = "Select JSON file.";
                        ofd.Filter = "JSON (*.json) | *.json| All Files (*.*) | *.*";
                        break;
                    }
                default:
                    {
                        ofd.Title = "Select file.";
                        ofd.Filter = "All Files (*.*) | *.*";
                        break;
                    }
            }

            if (ofd.ShowDialog() == DialogResult.OK && ofd.FileName.Length > 0)
            {
                return ofd.FileName;
            }
            else
                return null;
        }

    }
}