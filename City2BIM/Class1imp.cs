using Autodesk.Revit.DB;
using Autodesk.Revit.UI;



namespace City2BIM
{
    internal class Class1imp
    {
       

        public string ImportPath()
        {


            FileOpenDialog fileWin = new FileOpenDialog("CityGML-files (*.gml)|*.gml|All Files (*.*)|*.*");
            fileWin.Title = "Select CityGML file.";
            
            fileWin.Show();

            var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(fileWin.GetSelectedModelPath());


            return path;
        }
    }
}