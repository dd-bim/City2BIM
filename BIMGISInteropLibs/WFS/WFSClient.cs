using System;
using System.Text;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Globalization;

namespace BIMGISInteropLibs.WFS
{
    public class WFSClient
    {
        string wfsUrl { get; set; }

        public WFSClient(string url)
        {
            wfsUrl = url;
        }

        public XDocument getFeaturesCircle(double xCoord, double yCoord, double radius, int maxNrOfFeatures, string EPSGCode)
        {
            XDocument requestBody = getRequestBodyCircleFilter(xCoord, yCoord, radius, maxNrOfFeatures, EPSGCode);
            byte[] byteArray = Encoding.UTF8.GetBytes(requestBody.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.wfsUrl);

            request.Method = "POST";
            request.ContentType = "text/xml;charset=UTF-8";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();

            string responseServer;
            using (dataStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                responseServer = reader.ReadToEnd();
            }

            XDocument features = XDocument.Parse(responseServer);
            return features;
        }

        private static XDocument getRequestBodyCircleFilter(double xCoord, double yCoord, double radius, int maxNrOfFeatures, string EPSGCode)
        {
            XNamespace wfs = "http://www.opengis.net/wfs/2.0";
            XNamespace bldg = "http://www.opengis.net/citygml/building/2.0";
            XNamespace gml = "http://www.opengis.net/gml";
            XNamespace fes = "http://www.opengis.net/fes/2.0";

            XDocument requestBody = new XDocument(
                new XElement(wfs + "GetFeature",
                    new XAttribute(XNamespace.Xmlns + "gml", gml.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "bldg", bldg.NamespaceName),
                    new XAttribute("service", "WFS"),
                    new XAttribute("version", "2.0.0"),
                    //new XAttribute("outputFormat", "application/gml+xml; version=3.1"),
                    new XAttribute("count", maxNrOfFeatures),
                    new XElement(wfs + "Query",
                        new XAttribute("typeNames", "bldg:Building"),
                        new XAttribute("srsName", EPSGCode),
                        new XElement(fes + "Filter",
                            new XAttribute(XNamespace.Xmlns + "fes", fes.NamespaceName),
                            new XElement(fes + "DWithin",
                                new XElement(fes + "ValueReference", "gml:boundedBy"),
                                new XElement(gml + "Point",
                                    new XAttribute("srsName", "EPSG:4326"),
                                    new XElement(gml + "pos", String.Format(CultureInfo.InvariantCulture, "{0} {1}", xCoord, yCoord))
                                ),
                                new XElement(fes + "Distance",
                                    new XAttribute("uom", "m"), radius)
                             )
                         )
                    )
               )
           );

            return requestBody;
        }

        public XDocument getCapabilities()
        {
            XNamespace wfs = "http://www.opengis.net/wfs/2.0";

            XDocument requestBody = new XDocument(
                new XElement(wfs + "GetCapabilities",
                    new XAttribute("service", "WFS")
                )
            );

            Console.WriteLine(requestBody.ToString());

            byte[] reqAsByte = Encoding.UTF8.GetBytes(requestBody.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.wfsUrl);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=UTF-8";
            request.ContentLength = reqAsByte.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(reqAsByte, 0, reqAsByte.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();

            string responseServer;
            using (dataStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                responseServer = reader.ReadToEnd();
            }

            var capabilities = XDocument.Parse(responseServer);

            return capabilities;

        }

    }
}
