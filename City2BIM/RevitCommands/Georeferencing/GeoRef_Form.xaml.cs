using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.RevitBuilder;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace City2BIM.RevitCommands.Georeferencing
{
    /// <summary>
    /// Interaction logic for GeoRef_Form.xaml
    /// </summary>
    public partial class GeoRef_Form : Window
    {
        public const double radToDeg = 180 / Math.PI;
        public const double feetToM = 0.3048;

        public string[] epsgList = new string[] {   "EPSG:25831", "EPSG:25832", "EPSG:25833",
                                                    "EPSG:3043", "EPSG:3044", "EPSG:3045",
                                                    "EPSG:5649", "EPSG:4647", "EPSG:5650",
                                                    "EPSG:5651", "EPSG:5652", "EPSG:5653",
                                                    "EPSG:5554", "EPSG:5555", "EPSG:5556",};

        public string[] vertDatList = new string[] { "DHHN2016", "DHHN92", "DHHN85 (NN)", "SNN76 (HN)" };

        private ProjectLocation geoLoc;
        private string geoAddress;
        private SiteLocation geoSite;
        private ProjectPosition geoProject;
        private ProjectInfo geoInfo;
        private Document doc;

        public GeoRef_Form(ExternalCommandData revit)
        {
            UIApplication uiApp = revit.Application;
            this.doc = uiApp.ActiveUIDocument.Document;

            InitializeComponent();

            var path10 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon_georef10.png");
            Uri uri10 = new Uri(path10);
            BitmapImage img10 = new BitmapImage(uri10);
            georef10_pic.Source = img10;

            var path20 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon_georef20.png");
            Uri uri20 = new Uri(path20);
            BitmapImage img20 = new BitmapImage(uri20);
            georef20_pic.Source = img20;

            var path50 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon_georef50.png");
            Uri uri50 = new Uri(path50);
            BitmapImage img50 = new BitmapImage(uri50);
            georef50_pic.Source = img50;

            foreach (var item in epsgList)
            {
                cb_epsg.Items.Add(item);
            }

            foreach (var item in vertDatList)
            {
                cb_vertDatum.Items.Add(item);
            }

            this.geoInfo = doc.ProjectInformation;
            this.geoAddress = geoInfo.Address;
            this.geoSite = doc.SiteLocation;
            this.geoLoc = doc.ActiveProjectLocation;
            this.geoProject = geoLoc.GetProjectPosition(XYZ.Zero);

            #region Get Address

            //Adresse splitten???

            tb_adr0.Text = geoAddress;
            tb_adr1.Text = geoAddress;
            tb_plz.Text = geoAddress;
            tb_town.Text = geoAddress;
            tb_region.Text = geoAddress;
            tb_country.Text = geoAddress;

            #endregion Get Address

            #region Get SiteLocation

            tb_lat.Text = (geoSite.Latitude * radToDeg).ToString();
            tb_lon.Text = (geoSite.Longitude * radToDeg).ToString();
            tb_elev.Text = (geoSite.Elevation * feetToM).ToString();
            tb_trueNorth.Text = (geoProject.Angle * radToDeg).ToString();

            #endregion Get SiteLocation

            #region Get Project Location

            var east = geoInfo.LookupParameter("Eastings");
            if (east != null)
                tb_eastings50.Text = east.AsDouble().ToString();
            else
                tb_eastings50.Text = (geoProject.EastWest * feetToM).ToString();

            var north = geoInfo.LookupParameter("Northings");
            if (north != null)
                tb_northings50.Text = north.AsDouble().ToString();
            else
                tb_northings50.Text = (geoProject.NorthSouth * feetToM).ToString();

            var height = geoInfo.LookupParameter("OrthogonalHeight");
            if (height != null)
                tb_elev.Text = height.AsDouble().ToString();
            else
                tb_elev.Text = (geoProject.Elevation * feetToM).ToString();

            var rotAbs = geoInfo.LookupParameter("XAxisAbscissa");
            var rotOrd = geoInfo.LookupParameter("XAxisOrdinate");

            if (rotAbs != null && rotOrd != null)
                tb_rotation50.Text = (UTMcalc.VectorToAzimuth(rotAbs.AsDouble(), rotOrd.AsDouble()).ToString());

            var scale = geoInfo.LookupParameter("Scale");
            if (scale != null)
                tb_scale50.Text = scale.AsDouble().ToString();
            else
                tb_scale50.Text = "1";

            var epsg = geoInfo.LookupParameter("Name");
            if (epsg != null)
                cb_epsg.Text = epsg.AsString();

            var vertD = geoInfo.LookupParameter("VerticalDatum");
            if (vertD != null)
                cb_vertDatum.Text = vertD.AsString();

            #endregion Get Project Location
        }

        private void bt_calculate_Click(object sender, RoutedEventArgs e)
        {
            bool trDirProj = (bool)rb_siteToProj.IsChecked;

            double lat = double.NaN, lon = double.NaN;
            double east = double.NaN, north = double.NaN;
            double rotTrueN = double.NaN, rotGridN = double.NaN;
            int? zone = null;
            int zoneAdd = 0;

            var crsItem = cb_epsg.SelectedItem;

            if (crsItem == null)
            {
                TaskDialog.Show("Information", "Please select any predefined EPSG-Code first!");
            }
            else
            {
                var crs = crsItem.ToString();

                if (crs.Equals("EPSG:25831") || crs.Equals("EPSG:3043") || crs.Equals("EPSG:5649") || crs.Equals("EPSG:5651") || crs.Equals("EPSG:5554"))
                    zone = 31;
                if (crs.Equals("EPSG:25832") || crs.Equals("EPSG:3044") || crs.Equals("EPSG:4647") || crs.Equals("EPSG:5652") || crs.Equals("EPSG:5555"))
                    zone = 32;

                if (crs.Equals("EPSG:25833") || crs.Equals("EPSG:3045") || crs.Equals("EPSG:5650") || crs.Equals("EPSG:5653") || crs.Equals("EPSG:5556"))
                    zone = 33;

                if (crs.Equals(crs.Equals("EPSG:5649") || crs.Equals("EPSG:5651")))
                    zoneAdd = 31000000;
                if (crs.Equals(crs.Equals("EPSG:4647") || crs.Equals("EPSG:5652")))
                    zoneAdd = 32000000;
                if (crs.Equals(crs.Equals("EPSG:5650") || crs.Equals("EPSG:5653")))
                    zoneAdd = 33000000;


                if (trDirProj)
                {
                    lat = UTMcalc.StringToDeg(tb_lat.Text, (bool)rb_dms.IsChecked);
                    lon = UTMcalc.StringToDeg(tb_lon.Text, (bool)rb_dms.IsChecked);

                    rotTrueN = UTMcalc.ParseDouble(tb_trueNorth.Text);
                }
                else
                {
                    east = UTMcalc.ParseDouble(tb_eastings50.Text) - zoneAdd;
                    north = UTMcalc.ParseDouble(tb_northings50.Text);

                    rotGridN = UTMcalc.ParseDouble(tb_rotation50.Text);
                }

                double orthoHeight = UTMcalc.ParseDouble(tb_elev.Text);
                bool isSouth = false;

                UTMcalc.GetGeoRef(trDirProj, ref lat, ref lon, ref zone, ref east, ref north, ref isSouth, orthoHeight, trDirProj, ref rotTrueN, ref rotGridN, out double scale);

                tb_lat.Text = UTMcalc.DegToString(lat, (bool)rb_dms.IsChecked);
                tb_lon.Text = UTMcalc.DegToString(lon, (bool)rb_dms.IsChecked);
                tb_trueNorth.Text = UTMcalc.DoubleToString(rotTrueN, 9);

                tb_eastings50.Text = UTMcalc.DoubleToString(east + zoneAdd, 4);
                tb_northings50.Text = UTMcalc.DoubleToString(north, 4);

                tb_rotation50.Text = UTMcalc.DoubleToString(rotGridN, 9);
                tb_scale50.Text = UTMcalc.DoubleToString(scale, 9);

                tb_elev.Text = UTMcalc.DoubleToString(orthoHeight, 4);
            }
        }

        private void bt_applyGeoref_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            using (Transaction t = new Transaction(doc, "Apply georeferencing"))
            {
                t.Start();

                //TO DO: Address --> welche Form? nur eine Zeile, wie Revit-intern oder IFC-Klassen aus Exporter?

                #region Set SiteLocation

                var lat = UTMcalc.StringToDeg(tb_lat.Text, (bool)rb_dms.IsChecked);
                this.geoSite.Latitude = UTMcalc.DegToRad(lat);

                var lon = UTMcalc.StringToDeg(tb_lon.Text, (bool)rb_dms.IsChecked);
                this.geoSite.Longitude = UTMcalc.DegToRad(lon);

                //kein SETTER für SiteLocation.Elevation ???!!! --> nur PBP-Elevation nutzen? --> IFC-Test durchführen!

                //if(double.TryParse(tb_elev.Text, out var elev))
                //    this.GeoSite.Elevation = elev / feetToM;

                var trueN = UTMcalc.StringToDeg(tb_trueNorth.Text, (bool)rb_dms.IsChecked);
                this.geoProject.Angle = UTMcalc.DegToRad(trueN);

                //andere SiteLocation.Attribute setzen ? u.a auch GeoCoordinateSystem?

                #endregion Set SiteLocation

                #region Set ProjectLocation

                var east = UTMcalc.ParseDouble(tb_eastings50.Text);
                this.geoProject.EastWest = east / feetToM;

                var north = UTMcalc.ParseDouble(tb_northings50.Text);
                this.geoProject.NorthSouth = north / feetToM;

                var height = UTMcalc.ParseDouble(tb_elev.Text);
                this.geoProject.Elevation = height / feetToM;

                this.geoLoc.SetProjectPosition(XYZ.Zero, this.geoProject);

                t.Commit();
            }
            #endregion Set ProjectLocation

            #region Write Project Properties

            var sem = new RevitSemanticBuilder(doc);

            var vector = UTMcalc.AzimuthToVector(this.geoProject.Angle);

            var georefAttr = new Dictionary<string, double>()
                {
                    { "Eastings", (this.geoProject.EastWest * feetToM) },
                    { "Northings", (this.geoProject.NorthSouth * feetToM)},
                    { "OrthogonalHeight", (this.geoProject.Elevation * feetToM) },
                    { "XAxisAbscissa", vector[0] },
                    { "XAxisOrdinate", vector[1] },
                    { "Scale", UTMcalc.ParseDouble(tb_scale50.Text) }
                };

            var georefCRSAttr = new Dictionary<string, string>()
                {
                    { "Name", cb_epsg.Text },
                    { "VerticalDatum", cb_vertDatum.Text }
                };

            var mapAttributes = new Dictionary<string, GetSemantics.GmlAttribute.AttrType>()
            {
                    { "Eastings", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
                    { "Northings", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
                    { "OrthogonalHeight", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
                    { "XAxisAbscissa", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
                    { "XAxisOrdinate", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
                    { "Scale", GetSemantics.GmlAttribute.AttrType.doubleAttribute },
            };

            sem.CreateProjectParameters("ePSet_MapConversion", mapAttributes);

            var crsAttributes = new Dictionary<string, GetSemantics.GmlAttribute.AttrType>()
            {
                    { "Name", GetSemantics.GmlAttribute.AttrType.stringAttribute },
                    { "Description", GetSemantics.GmlAttribute.AttrType.stringAttribute  },
                    { "GeodeticDatum", GetSemantics.GmlAttribute.AttrType.stringAttribute  },
                    { "VerticalDatum", GetSemantics.GmlAttribute.AttrType.stringAttribute  },
                    { "MapProjection", GetSemantics.GmlAttribute.AttrType.stringAttribute  },
                    { "MapZone", GetSemantics.GmlAttribute.AttrType.stringAttribute  },
            };

            sem.CreateProjectParameters("ePSet_ProjectedCRS", crsAttributes);

            using (Transaction t = new Transaction(doc, "Apply georeferencing to Project Info"))
            {
                t.Start();

                var proj = doc.ProjectInformation;

                foreach (var aName in mapAttributes.Keys)
                {
                    var p = proj.LookupParameter(aName);
                    georefAttr.TryGetValue(aName, out var val);

                    try
                    {
                        p.Set(val);
                    }
                    catch
                    {
                        Log.Error("Semantik-Fehler bei " + aName);
                        continue;
                    }

                }

                foreach (var aName in crsAttributes.Keys)
                {
                    var p = proj.LookupParameter(aName);
                    georefCRSAttr.TryGetValue(aName, out var val);

                    if (val != null)
                    {

                        try
                        {
                            p.Set(val);
                        }
                        catch
                        {
                            Log.Error("Semantik-Fehler bei " + aName);
                            continue;
                        }
                    }
                }
                //sem.CreateParameterSetFile();

                t.Commit();
            }
            #endregion Write Project Properties
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Cb_epsg_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var crs = cb_epsg.SelectedItem;

            if (crs != null)
                if (crs.ToString().Equals("EPSG:5554") || crs.ToString().Equals("EPSG:5555") || crs.ToString().Equals("EPSG:5556"))
                    cb_vertDatum.SelectedIndex = cb_vertDatum.Items.IndexOf("DHHN92");
                else
                    cb_vertDatum.SelectedIndex = -1;
        }

        private void Rb_deg_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tb_lat.Text))
            {
                var val = UTMcalc.StringToDeg(tb_lat.Text, (bool)rb_deg.IsChecked);
                tb_lat.Text = UTMcalc.DegToString(val, (bool)rb_dms.IsChecked);
            }
            if (!string.IsNullOrWhiteSpace(tb_lon.Text))
            {
                var val = UTMcalc.StringToDeg(tb_lon.Text, (bool)rb_deg.IsChecked);
                tb_lon.Text = UTMcalc.DegToString(val, (bool)rb_dms.IsChecked);
            }
        }
    }
}