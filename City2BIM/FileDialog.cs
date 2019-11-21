using System.Windows.Forms;

namespace City2BIM
{
    internal class FileDialog
    {
        public enum Data { CityGML, ALKIS, DGM };

        public string ImportPath(Data geodata)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            switch (geodata)
            {
                case (Data.CityGML):
                    {
                        ofd.Title = "Select CityGML file.";
                        ofd.Filter = "CityGML (*.gml) | *.gml* | CityGML (*.xml) | *.xml* | All Files (*.*) | *.*";
                        break;
                    }
                case (Data.ALKIS):
                    {
                        ofd.Title = "Select NAS-ALKIS file.";
                        ofd.Filter = "NAS-XML |*.xml | All Files (*.*) | *.*";
                        break;
                    }
                case (Data.DGM):
                    {
                        ofd.Title = "Select Grid terrain file.";
                        ofd.Filter = "DGM - XYZ coordinates (*.txt) | *.txt | DGM - XYZ coordinates (*.csv) | *.csv | All Files (*.*) | *.*";
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