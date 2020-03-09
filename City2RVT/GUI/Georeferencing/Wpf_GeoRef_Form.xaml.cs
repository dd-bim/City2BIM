using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.Semantic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using City2RVT.Calc;

namespace City2RVT.GUI
{
    /// <summary>
    /// Interaction logic for GeoRef_Form.xaml
    /// </summary>
    public partial class Wpf_GeoRef_Form : Window
    {
        public const double radToDeg = 180 / Math.PI;
        public const double feetToM = 0.3048;

        public string[] epsgList = new string[] {   "EPSG:25832", "EPSG:25833"};

        public string[] vertDatList = new string[] { "DHHN2016", "DHHN92", "DHHN85 (NN)", "SNN76 (HN)" };

        //private ProjectLocation geoLoc;
        private readonly string geoAddress;
        //private SiteLocation geoSite;
        //private ProjectPosition geoProject;
        private readonly ProjectInfo geoInfo;
        private readonly Document doc;

        public Wpf_GeoRef_Form(Document doc)
        {
            this.doc = doc;

            InitializeComponent();

            //var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GUI");
            //path = Path.Combine(path, "")

            /*var path10 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\icon_georef10.png");
            Uri uri10 = new Uri(path10);
            BitmapImage img10 = new BitmapImage(uri10);*/
            System.IO.MemoryStream ms10 = new System.IO.MemoryStream();
            var bitmap10 = ResourcePictures.Georef_32px_96dpi;
            bitmap10.Save(ms10, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage img10 = new BitmapImage();
            ms10.Position = 0;
            img10.BeginInit();
            img10.StreamSource = ms10;
            img10.EndInit();
            georef10_pic.Source = img10;

            /*var path20 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\icon_georef20.png");
            Uri uri20 = new Uri(path20);
            BitmapImage img20 = new BitmapImage(uri20);*/
            System.IO.MemoryStream ms20 = new System.IO.MemoryStream();
            var bitmap20 = ResourcePictures.Georef_32px_96dpi;
            bitmap20.Save(ms20, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage img20 = new BitmapImage();
            ms20.Position = 0;
            img20.BeginInit();
            img20.StreamSource = ms20;
            img20.EndInit();
            georef20_pic.Source = img20;

            /*var path50 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\icon_georef50.png");
            Uri uri50 = new Uri(path50);
            BitmapImage img50 = new BitmapImage(uri50);*/
            System.IO.MemoryStream ms50 = new System.IO.MemoryStream();
            var bitmap50 = ResourcePictures.Georef_32px_96dpi;
            bitmap50.Save(ms50, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage img50 = new BitmapImage();
            ms50.Position = 0;
            img50.BeginInit();
            img50.StreamSource = ms50;
            img50.EndInit();
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

            tb_lat.Text = Prop_GeoRefSettings.WgsCoord[0].ToString();
            tb_lon.Text = Prop_GeoRefSettings.WgsCoord[1].ToString();
            tb_elev.Text = Prop_GeoRefSettings.ProjElevation.ToString();
            tb_trueNorth.Text = Prop_GeoRefSettings.ProjAngle.ToString();

            #endregion Get SiteLocation

            #region Get Project Location

            tb_eastings50.Text = Prop_GeoRefSettings.ProjCoord[1].ToString();
            tb_northings50.Text = Prop_GeoRefSettings.ProjCoord[0].ToString();
            tb_elev.Text = Prop_GeoRefSettings.ProjElevation.ToString();

            tb_scale50.Text = Prop_GeoRefSettings.ProjScale.ToString();


            //Information parameter: Grid North
            var rotAbs = geoInfo.LookupParameter("XAxisAbscissa");
            var rotOrd = geoInfo.LookupParameter("XAxisOrdinate");
            if (rotAbs != null && rotOrd != null)
                tb_rotation50.Text = (UTMcalc.VectorToAzimuth(rotAbs.AsDouble(), rotOrd.AsDouble()).ToString());

            //CRS from settings
            cb_epsg.Text = Prop_GeoRefSettings.Epsg;
                
           //Information parameter: Vertical Datum
            
            var vertD = geoInfo.LookupParameter("VerticalDatum");
            if (vertD != null)
                cb_vertDatum.Text = vertD.AsString();

            #endregion Get Project Location
        }

        private void Bt_calculate_Click(object sender, RoutedEventArgs e)
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

                if (crs.Equals("EPSG:25832") || crs.Equals("EPSG:3044") || crs.Equals("EPSG:4647") || crs.Equals("EPSG:5652") || crs.Equals("EPSG:5555"))
                {
                    zone = 32;
                    Prop_GeoRefSettings.Epsg = "EPSG:25832";
                }
                    

                if (crs.Equals("EPSG:25833") || crs.Equals("EPSG:3045") || crs.Equals("EPSG:5650") || crs.Equals("EPSG:5653") || crs.Equals("EPSG:5556"))
                {
                    zone = 33;
                    Prop_GeoRefSettings.Epsg = "EPSG:25833";
                }


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

            Prop_GeoRefSettings.IsGeoreferenced = true;
        }

        private void Bt_applyGeoref_Click(object sender, RoutedEventArgs e)
        {
            var geoSite = doc.SiteLocation;
            var geoLoc = doc.ActiveProjectLocation;
            var geoProject = geoLoc.GetProjectPosition(XYZ.Zero);

            //try
            //{
            using (Transaction t = new Transaction(doc, "Apply georeferencing"))
            {
                t.Start();

                //TO DO: Address --> welche Form? nur eine Zeile, wie Revit-intern oder IFC-Klassen aus Exporter?

                #region Set SiteLocation

                var lat = UTMcalc.StringToDeg(tb_lat.Text, (bool)rb_dms.IsChecked);
                geoSite.Latitude = UTMcalc.DegToRad(lat);

                var lon = UTMcalc.StringToDeg(tb_lon.Text, (bool)rb_dms.IsChecked);
                geoSite.Longitude = UTMcalc.DegToRad(lon);

                //kein SETTER für SiteLocation.Elevation ???!!! --> nur PBP-Elevation nutzen? --> IFC-Test durchführen!

                //if(double.TryParse(tb_elev.Text, out var elev))
                //    this.GeoSite.Elevation = elev / feetToM;

                var trueN = UTMcalc.StringToDeg(tb_trueNorth.Text, (bool)rb_dms.IsChecked);
                geoProject.Angle = UTMcalc.DegToRad(trueN);

                //andere SiteLocation.Attribute setzen ? u.a auch GeoCoordinateSystem?

                #endregion Set SiteLocation

                #region Set ProjectLocation

                var east = UTMcalc.ParseDouble(tb_eastings50.Text);
                geoProject.EastWest = east / feetToM;

                var north = UTMcalc.ParseDouble(tb_northings50.Text);
                geoProject.NorthSouth = north / feetToM;

                var height = UTMcalc.ParseDouble(tb_elev.Text);
                geoProject.Elevation = height / feetToM;

                geoLoc.SetProjectPosition(XYZ.Zero, geoProject);

                t.Commit();
            }
            #endregion Set ProjectLocation

            #region Write Project Properties

            var sem = new Builder.Revit_Semantic(doc);

            var vector = UTMcalc.AzimuthToVector(geoProject.Angle);

            var georefAttr = new Dictionary<string, double>()
                {
                    { "Eastings", (geoProject.EastWest * feetToM) },
                    { "Northings", (geoProject.NorthSouth * feetToM)},
                    { "OrthogonalHeight", (geoProject.Elevation * feetToM) },
                    { "XAxisAbscissa", vector[0] },
                    { "XAxisOrdinate", vector[1] },
                    { "Scale", UTMcalc.ParseDouble(tb_scale50.Text) }
                };

            var georefCRSAttr = new Dictionary<string, string>()
                {
                    { "Name", cb_epsg.Text },
                    { "VerticalDatum", cb_vertDatum.Text }
                };

            var mapAttributes = new Dictionary<string, Xml_AttrRep.AttrType>()
            {
                    { "Eastings", Xml_AttrRep.AttrType.doubleAttribute },
                    { "Northings", Xml_AttrRep.AttrType.doubleAttribute },
                    { "OrthogonalHeight", Xml_AttrRep.AttrType.doubleAttribute },
                    { "XAxisAbscissa", Xml_AttrRep.AttrType.doubleAttribute },
                    { "XAxisOrdinate", Xml_AttrRep.AttrType.doubleAttribute },
                    { "Scale", Xml_AttrRep.AttrType.doubleAttribute },
            };

            sem.CreateProjectParameters("ePSet_MapConversion", mapAttributes);

            var crsAttributes = new Dictionary<string, Xml_AttrRep.AttrType>()
            {
                    { "Name", Xml_AttrRep.AttrType.stringAttribute },
                    { "Description", Xml_AttrRep.AttrType.stringAttribute  },
                    { "GeodeticDatum", Xml_AttrRep.AttrType.stringAttribute  },
                    { "VerticalDatum", Xml_AttrRep.AttrType.stringAttribute  },
                    { "MapProjection", Xml_AttrRep.AttrType.stringAttribute  },
                    { "MapZone", Xml_AttrRep.AttrType.stringAttribute  },
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
                            continue;
                        }
                    }
                }
                //sem.CreateParameterSetFile();

                t.Commit();
            }
            #endregion Write Project Properties

            Prop_GeoRefSettings.SetInitialSettings(doc);    //upates ImportSettings with new GeoRef data
            Prop_GeoRefSettings.ProjScale = UTMcalc.ParseDouble(tb_scale50.Text);
            Prop_GeoRefSettings.Epsg = cb_epsg.Text;
        }

        private void Bt_quit_Click(object sender, RoutedEventArgs e)
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
                var val = Calc.UTMcalc.StringToDeg(tb_lat.Text, (bool)rb_deg.IsChecked);
                tb_lat.Text = Calc.UTMcalc.DegToString(val, (bool)rb_dms.IsChecked);
            }
            if (!string.IsNullOrWhiteSpace(tb_lon.Text))
            {
                var val = Calc.UTMcalc.StringToDeg(tb_lon.Text, (bool)rb_deg.IsChecked);
                tb_lon.Text = UTMcalc.DegToString(val, (bool)rb_dms.IsChecked);
            }
        }
    }
}