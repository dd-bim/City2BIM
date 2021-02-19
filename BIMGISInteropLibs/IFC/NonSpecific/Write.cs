using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.IO;                                  //Enumeration to XbimStoreType //verlagert

namespace BIMGISInteropLibs.IFC.NonSpecific
{
    public static class Write
    {
        /// <summary>
        /// Writing the file
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fileName"></param>
        /// <param name="asXML"></param>
        public static void WriteFile(IfcStore model, string fileName, bool asXML = false)
        {
            if (asXML)
            { model.SaveAs(fileName, StorageType.IfcXml); }
            else
            { model.SaveAs(fileName, StorageType.Ifc); }
        }
    }
}
