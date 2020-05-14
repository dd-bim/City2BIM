using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using City2BIM.Alkis;
using City2BIM.Geometry;
using System.Globalization;
using System.Xml.Linq;
using City2BIM;
using System.Xml;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace City2RVT.Reader
{
    public class ReadXPlan
    {
        /// <summary>
        /// Receives all feature member of xplan document and saves them to a list
        /// </summary>
        /// <param name="allXPlanObjects"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public List<string> getXPlanFeatureMembers(XmlNodeList allXPlanObjects, XmlNamespaceManager nsmgr)
        {
            List<string> xPlanObjectList = new List<string>();
            foreach (XmlNode x in allXPlanObjects)
            {
                if (x.FirstChild.SelectNodes(".//xplan:position", nsmgr) != null)
                {
                    if (xPlanObjectList.Contains(x.FirstChild.Name.ToString()) == false)
                    {
                        if (x.FirstChild.Name.ToString() == "xplan:BP_Bereich")
                        {
                            xPlanObjectList.Insert(0, x.FirstChild.Name.ToString());
                        }
                        else if (x.FirstChild.Name.ToString() == "xplan:BP_Plan")
                        {
                            xPlanObjectList.Insert(0, x.FirstChild.Name.ToString());
                        }
                        else
                        {
                            xPlanObjectList.Add(x.FirstChild.Name.ToString());
                        }
                    }
                }
            }
            return xPlanObjectList;
        }

        /// <summary>
        /// Receives all parameters of xplan document and saves them to a list
        /// </summary>
        /// <param name="allXPlanObjects"></param>
        /// <returns></returns>
        public List<string> getXPlanParameter(string layer, XmlDocument xmlDoc, XmlNamespaceManager nsmgr)
        {
            var selectedLayer = GUI.Prop_NAS_settings.SelectedLayer;

            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember/" + layer, nsmgr);


            List<string> allParamList = new List<string>();
            foreach (XmlNode xmlNode in allXPlanObjects)
            {
                foreach (XmlNode child in xmlNode)
                {
                    if (child.Name != "#comment")
                    {
                        if (allParamList.Contains(child.Name) == false)
                        {
                            allParamList.Add(child.Name);
                        }
                    }
                }
            }
            return allParamList;
        }

        public CurveLoop getInterior(XmlNode interiorNode, XmlNamespaceManager nsmgr, List<string> interiorListe, Document doc, Autodesk.Revit.ApplicationServices.Application app, Transform transf, double R, double zOffset, int ii)
        {
            CurveLoop curveLoopInterior = new CurveLoop();

            {
                XmlNodeList interiorNodeList = interiorNode.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                XmlNodeList interiorRingNodeList = interiorNode.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                foreach (XmlNode xc in interiorNodeList)
                {
                    interiorListe.Add(xc.InnerText);
                    string[] koordWerteInterior = interiorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (koordWerteInterior.Count() == 4)
                    {
                        var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                        Line lineExterior = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                        curveLoopInterior.Append(lineExterior);
                    }

                    else if (koordWerteInterior.Count() > 4)
                    {
                        int ia = 0;

                        foreach (string split in koordWerteInterior)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                            Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, ia, zOffset);
                            curveLoopInterior.Append(lineClIndu);

                            if ((ia + 3) == (koordWerteInterior.Count() - 1))
                            {
                                break;
                            }
                            ia += 2;
                        }
                    }
                    ii++;
                }

                foreach (XmlNode xc in interiorRingNodeList)
                {
                    interiorListe.Add(xc.InnerText);
                    string[] koordWerteInterior = interiorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (koordWerteInterior.Count() == 4)
                    {
                        var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                        Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                        curveLoopInterior.Append(lineStrasse);
                    }

                    else if (koordWerteInterior.Count() > 4)
                    {
                        int ib = 0;
                        foreach (string split in koordWerteInterior)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                            Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, ib, zOffset);
                            curveLoopInterior.Append(lineClIndu);

                            if ((ib + 3) == (koordWerteInterior.Count() - 1))
                            {
                                break;
                            }

                            ib += 2;
                        }
                    }
                    ii++;
                }
            }
            return curveLoopInterior;
        }
    }
}
