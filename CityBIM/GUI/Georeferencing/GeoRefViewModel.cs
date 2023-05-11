using System;

using Autodesk.Revit.DB;
using Serilog;

using CommonRevit;
using System.Diagnostics;

namespace CityBIM.GUI.Georeferencing
{
    public class GeoRefViewModel : BaseViewModel
    {
        private double eastings;
        public double Eastings
        {
            get => eastings;
            set
            {
                if (eastings != value)
                {
                    eastings = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        private double northings;
        public double Northings
        {
            get => northings;
            set
            {
                if (northings != value)
                {
                    northings = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        private double elevation;
        public double Elevation
        {
            get => elevation;
            set
            {
                if (elevation != value)
                {
                    elevation = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        
        private double trueNorth;
        public double TrueNorth
        {
            get => trueNorth;
            set
            {
                if (trueNorth != value)
                {
                    trueNorth = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        private int epsgCode;
        public int EPSGCode
        {
            get => epsgCode;
            set
            {
                if (epsgCode != value)
                {
                    epsgCode = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private double projectScale;

        public double ProjectScale
        {
            get => projectScale;
            set
            {
                if (projectScale != value)
                {
                    projectScale = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private Document doc;
        
        public GeoRefViewModel(Document doc)
        {
            this.doc = doc;
            var pbp = utils.getProjectBasePointMeter(doc);
            this.Eastings = pbp.X;
            this.Northings = pbp.Y;
            this.Elevation = pbp.Z;
            this.TrueNorth = utils.getProjectAngleDeg(doc);
            this.EPSGCode = utils.getHTWDDEPSGCode(doc);
            this.ProjectScale = utils.getHTWDDProjectScale(doc);
        }

        public GeoRefViewModel()
        {

        }

        public void SaveSettings()
        {
            var pbpOld = utils.getProjectBasePointMeter(this.doc);
            var angleOld = utils.getProjectAngleDeg(this.doc);
            var scaleOld = utils.getHTWDDProjectScale(this.doc);

            utils.storeEPSGCodeInExtensibleStorage(this.doc, this.EPSGCode);
            utils.storeProjectScaleInExtensibleStorage(this.doc, this.ProjectScale);

            using (Transaction trans = new Transaction(this.doc, "Setting Coordinates of project base point"))
            {
                trans.Start();
                this.doc.ActiveProjectLocation.SetProjectPosition(new XYZ(), new ProjectPosition(Eastings / 0.3048, Northings / 0.3048, Elevation / 0.3048, TrueNorth * Math.PI/180));
                trans.Commit();
            }

            Log.Information($"New Project location is: {Eastings / 0.3048} | {Northings / 0.3048} | {Elevation / 0.3048}     Old Location was: {pbpOld.X} | {pbpOld.Y} | {pbpOld.Z}");
            Log.Information($"New True North is: {TrueNorth * Math.PI / 180}        Old True North was: {angleOld}");
            Log.Information($"New Projece Scale is: {ProjectScale}      Old Project Scale was: {scaleOld}");
        }
    }
}
