using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using System.ComponentModel;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace NasImport

{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class ReadXml
    {
        ExternalCommandData commandData;


        //static void Main(string[] args)
       
        public void ReadXmlTest( )
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            // Load the document and set the root element.  
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Bestandsdaten_VERMESSUNGSSTELLEN_Auszug.XML");
            //XPathNavigator navigator = doc.CreateNavigator();
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns1", "http://www.ibr-bonn.de/ibr");
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("adv", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gco", "http://www.isotc211.org/2005/gco");
            nsmgr.AddNamespace("gmd", "http://www.isotc211.org/2005/gmd");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("ows", "http://www.opengis.net/ows");
            nsmgr.AddNamespace("wfs", "http://www.adv-online.de/namespaces/adv/gid/wfs");
            nsmgr.AddNamespace("wfsext", "http://www.adv-online.de/namespaces/adv/gid/wfsext");
            nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            nsmgr.AddNamespace("ogc", "http://www.adv-online.de/namespaces/adv/gid/ogc");

            XmlNodeList fKoord = xmlDoc.SelectNodes("//ns2:AX_Flurstueck/ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);

            List<string> list = new List<String>();
            foreach (XmlNode node in fKoord)
            {
                list.Add(node.InnerText);
            }
            string[] ssize = list[1].Split(' ');
            Console.WriteLine(ssize[0] + ' ' + ssize[1] + ' ' + ' ' + ssize[2] + ' ' + ssize[3]);



            //documentTransaction.Start();
            //Autodesk.Revit.UI.UIApplication revit = commandData.Application;


            XYZ startPoint;
            XYZ endPoint;
            //int modelLineId;
            //int sketchPlaneId;

            double startX = Convert.ToDouble(ssize[0]);
            double startXm = startX * 3.2808333;
            double startY = Convert.ToDouble(ssize[1]);
            double startYm = startY * 3.2808333;
            double startZ = 0.0;
            double startZm = startZ * 3.2808333;

            double endX = Convert.ToDouble(ssize[2]);
            double endXm = endX * 3.2808333;
            double endY = Convert.ToDouble(ssize[3]);
            double endYm = endY * 3.2808333;
            double endZ = 0.0;
            double endZm = endZ * 3.2808333;

            startPoint = new XYZ(startXm, startYm, startZm);
            endPoint = new XYZ(endXm, endYm, endZm);
            Line geomLine = Line.CreateBound(startPoint, endPoint);

            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, 3.2808333);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            Transaction documentTransaction = new Transaction(doc, "Create Model Line");
            {
                documentTransaction.Start();
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                ModelLine line = doc.Create.NewModelCurve(geomLine, sketch) as ModelLine;
            }



            //documentTransaction.Commit();
            /*sketchPlaneId = 1;
            


            m_dataBuffer.CreateLine(sketchPlaneId, startPoint, endPoint);
            */

            //this.Refresh();

            MessageBox.Show("ALKIS-XML eingelesen!");
            documentTransaction.Commit();

        }
    }
    /*public List<string> ReadXmlTest()
    {
        //MessageBox.Show("Test bestanden!");

        XmlDocument doc = new XmlDocument();
        doc.Load("TestXml.XML");

        XmlNodeList xtNode = doc.SelectNodes("//Punkte");
        List<string> list = new List<string>();

        foreach (XmlNode itemNode in xtNode)
        {
            XmlNode coordNode = itemNode.SelectSingleNode("Punkt");
            Console.WriteLine(coordNode.InnerText);
            string[] ssize = coordNode.InnerText.Split(' ');
            Console.WriteLine(ssize);

            list.Add(ssize[0]);
            list.Add(ssize[1]);
            list.Add(ssize[2]);

        }
        MessageBox.Show(list[1]);


        return list;


    }*/

}

