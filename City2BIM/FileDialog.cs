using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace City2BIM
{
    internal class FileDialog
    {
        public string ImportPathCityGML()
        {
            FileOpenDialog fileWin = new FileOpenDialog("CityGML-files (*.gml)|*.gml|All Files (*.*)|*.*");
            fileWin.Title = "Select CityGML file.";

            fileWin.Show();

            var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(fileWin.GetSelectedModelPath());

            return path;
        }

        public string ImportPathXYZ()
        {
            FileOpenDialog fileWin =
                new FileOpenDialog("DGM - XYZ coordinates (*.txt)|*.txt|DGM - XYZ coordinates (*.csv)|*.csv|All Files (*.*)|*.*");
            fileWin.Title = "Select Terrain file with XYZ data.";

            fileWin.Show();

            var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(fileWin.GetSelectedModelPath());

            return path;
        }
    }
}