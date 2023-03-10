using IFCGeorefShared;
using Xbim.Ifc;

namespace IFCGeoRefCheckerCommand
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string ifcFilePath = @"D:\Testdaten\GeoRefChecker\Buerogebaeude.ifc";
            //string ifcFilePath = @"D:\Testdaten\GeoRefChecker\301110Gebaeude-Gruppe.ifc";
            string ifcFilePath = @"D:\Testdaten\GeoRefChecker\Ausgangsprojekt_Wand_edit_Lev50_ifc4.ifc";

            using (var model = IfcStore.Open(ifcFilePath))
            {
                var checker = new GeoRefChecker(model);

                checker.WriteProtocoll(@"D:\TestDaten\GeoRefChecker\IfcGeoRefChecker");
            }

            Console.WriteLine("Check finished");
        }
    }
}