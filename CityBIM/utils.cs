using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;

using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;
using OSGeo.GDAL;
using OSGeo.OGR;
using Serilog;

namespace CityBIM
{
    public class utils
    {

        public static Schema getSchemaByName(string schemaName)
        {
            var schemaList = Schema.ListSchemas();
            foreach (var schema in schemaList)
            {
                if (schema.SchemaName == schemaName)
                {
                    return schema;
                }
            }
            return null;
        }

        public static Dictionary<string, string> getIfc2CityGMLGuidDic(Document doc)
        {
            Dictionary<string, string> IfcToRevitDic = new Dictionary<string, string>();

            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Entourage);
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            IList<Element> selection = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();

            var cityGMLSchema = utils.getSchemaByName("CityGMLImportSchema");
            foreach (Element el in selection)
            {
                Entity ent = el.GetEntity(cityGMLSchema);

                if (ent != null && ent.IsValidObject)
                {
                    Guid cShaprGuid = ExportUtils.GetExportId(doc, el.Id);
                    string ifcGuid = IfcGuid.ToIfcGuid(cShaprGuid);
                    IfcToRevitDic.Add(ifcGuid, el.UniqueId);
                }
            }

            return IfcToRevitDic;
        }

        public static ElementId getHTWDDTerrainID(Document doc)
        {
            using (Transaction trans = new Transaction(doc, "Read TerrainID"))
            {
                trans.Start();
                Schema terrainIDSchema = getSchemaByName("HTWDD_TerrainID");

                if (terrainIDSchema == null)
                {
                    trans.Commit();
                    return null;
                }

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

                foreach (var ds in dataStorageList)
                {
                    Entity ent = ds.GetEntity(terrainIDSchema);

                    if (ent.IsValid())
                    {
                        ElementId terrainID =  ent.Get<ElementId>(terrainIDSchema.GetField("terrainID"));
                        var element = doc.GetElement(terrainID);
                        
                        //if user deleted a former dtm the id still exits in the dataStorage, so make sure the dtm still exists in the project,
                        //otherwise return null
                        if (element != null) {
                            trans.Commit();
                            return terrainID;
                        }
                    }
                }
            }
            return null;
        }

        public static double getHTWDDProjectScale(Document doc)
        {
            Schema scaleSchema = getSchemaByName("HTWDD_ProjectScale");
            if (scaleSchema == null) { return 1.0; }

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

            foreach (var ds in dataStorageList)
            {
                Entity ent = ds.GetEntity(scaleSchema);

                if (ent.IsValid())
                {
                    double scale = ent.Get<double>(scaleSchema.GetField("ProjectScale"), UnitTypeId.Custom);

                    if (scale != 1.0) { return scale; }
                    else { return 1.0;}
                }
            }

            return 1.0;
        }

        public static void storeProjectScaleInExtensibleStorage(Document doc, double Scale)
        {
            using (Transaction trans = new Transaction(doc, "Store Project Scale"))
            {
                trans.Start();
                Schema projectScaleSchema = getSchemaByName("HTWDD_ProjectScale");

                if (projectScaleSchema == null)
                {
                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName("HTWDD_ProjectScale");
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);

                    FieldBuilder fb = sb.AddSimpleField("ProjectScale", typeof(double));
                    var unitsNeeded = fb.NeedsUnits();
                    fb.SetDocumentation("Field stores project scale used for UTM coordinate calculations");
                    fb.SetSpec(SpecTypeId.Custom);
                    projectScaleSchema = sb.Finish();
                }

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

                if (dataStorageList.Count > 0)
                {
                    foreach (var ds in dataStorageList)
                    {
                        var existingEnt = ds.GetEntity(projectScaleSchema);
                        if (existingEnt.IsValid())
                        {
                            ds.DeleteEntity(projectScaleSchema);
                        }
                    }
                }

                Entity ent = new Entity(projectScaleSchema);
                Field projectScaleField = projectScaleSchema.GetField("ProjectScale");
                ent.Set<double>(projectScaleField, Scale, UnitTypeId.Custom);

                DataStorage scaleIDStorage = DataStorage.Create(doc);
                scaleIDStorage.SetEntity(ent);

                trans.Commit();
            }
        }

        public static XYZ getProjectBasePointMeter(Document doc)
        {
            var projectBasePointFeet = BasePoint.GetProjectBasePoint(doc);
            return projectBasePointFeet.SharedPosition.Multiply(0.3048);
        }

        public static double getProjectAngleDeg(Document doc)
        {
            ProjectPosition projectPositionOrigin = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
            return projectPositionOrigin.Angle * 180 / System.Math.PI;
        }

        public static Transform getGlobalToRevitTransform(Document doc)
        {
            var pbp_feet = BasePoint.GetProjectBasePoint(doc).SharedPosition;
            var angle_rad = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero).Angle;

            var trafoRot = Transform.CreateRotation(XYZ.BasisZ, -angle_rad);
            var trafoTrans = Transform.CreateTranslation(-pbp_feet);
            var trafo = trafoRot.Multiply(trafoTrans);

            return trafo;
        }

        public static List<Dictionary<string, Dictionary<string, string>>> getSchemaAttributesForElement(Element element)
        {
            List<Dictionary<string, Dictionary<string, string>>> schemaAndAttributeList = new List<Dictionary<string, Dictionary<string, string>>>();

            var schemaGUIDList = element.GetEntitySchemaGuids();

            foreach (var schemaGUID in schemaGUIDList)
            {
                var schema = Schema.Lookup(schemaGUID);

                Entity ent = element.GetEntity(schema);

                Dictionary<string, string> attrDict = new Dictionary<string, string>();
                foreach (var field in schema.ListFields())
                {
                    var value = ent.Get<string>(field);
                    if (value != null && value != string.Empty)
                    {
                        attrDict.Add(field.FieldName, value);
                    }
                }

                schemaAndAttributeList.Add(new Dictionary<string, Dictionary<string, string>>() { { schema.SchemaName, attrDict } });
            }

            return schemaAndAttributeList;
        }

        public static bool configureOgr()
        {
            string executingAssemblyFile = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
            string executingDirectory = Path.GetDirectoryName(executingAssemblyFile);

            if (string.IsNullOrEmpty(executingDirectory))
            {
                Log.Error("cannot get executing directory");
                throw new InvalidOperationException("cannot get executing directory");
            }
                

            string gdalPath = Path.Combine(executingDirectory, "gdal");
            string nativePath = Path.Combine(gdalPath, "x64");
            if (!Directory.Exists(nativePath))
            {
                Log.Error("Did not found GDAL-Directory!");
                throw new DirectoryNotFoundException($"GDAL native directory not found at '{nativePath}'");
            }
                
            if (!File.Exists(Path.Combine(nativePath, "gdal_wrap.dll")))
            {
                Log.Error("Could not find gdal_wrap.dll in directory: " + nativePath);
                throw new FileNotFoundException(
                    $"GDAL native wrapper file not found at '{Path.Combine(nativePath, "gdal_wrap.dll")}'");
            }
                

            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + nativePath);

            // Set the additional GDAL environment variables.
            string gdalData = Path.Combine(gdalPath, "data");
            Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
            Gdal.SetConfigOption("GDAL_DATA", gdalData);

            string driverPath = Path.Combine(nativePath, "plugins");
            Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath);
            Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath);

            Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
            Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

            string projSharePath = Path.Combine(gdalPath, "share");
            Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath);
            Gdal.SetConfigOption("PROJ_LIB", projSharePath);
            OSGeo.OSR.Osr.SetPROJSearchPaths(new[] { projSharePath });

            Ogr.RegisterAll();

            return true;
        }

        public static List<Schema> getHTWSchemas(Document doc)
        {
            var schemaList = Schema.ListSchemas();
            List<Schema> htwSchemas = new List<Schema>();
            foreach (var s in schemaList)
            {
                if (s.VendorId == "HTWDRESDEN")
                {
                    htwSchemas.Add(s);
                }
            }
            return htwSchemas;
        }

        public static DataStorage getRefPlaneDataStorageObject(Document doc)
        {
            DataStorage refPlaneDataStorage;
            var refPlaneSchema = utils.getSchemaByName("HTWDD_RefPlaneSchema");

            if (refPlaneSchema == null)
            {
                using (Transaction trans = new Transaction (doc, "create Schema"))
                {
                    trans.Start();
                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName("HTWDD_RefPlaneSchema");
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);
                    sb.SetVendorId("HTWDresden");

                    sb.AddMapField("RefPlaneElementIdToString", typeof(ElementId), typeof(string));

                    refPlaneSchema = sb.Finish();
                    trans.Commit();
                }
            }

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ExtensibleStorageFilter filter = new ExtensibleStorageFilter(refPlaneSchema.GUID);

            var resultElements = collector.WherePasses(filter).ToList();

            if (resultElements.Count > 0)
            {
                return resultElements.FirstOrDefault() as DataStorage;
            }

            else
            {
                using (Transaction trans = new Transaction(doc, "getOrCreateRefPlaneObject"))
                {
                    trans.Start();
                    Entity ent = new Entity(refPlaneSchema);
                    refPlaneDataStorage = DataStorage.Create(doc);
                    refPlaneDataStorage.SetEntity(ent);
                    trans.Commit();
                }
                return refPlaneDataStorage;
            }
        }


        /// <summary>
        /// Enumeration for supported revit version <para/>
        /// UPDATE ME: if a Revit version is added or no longer supported
        /// </summary>
        public enum rvtVersion
        {
            R20 = 2020,
            R21 = 2021,
            R22 = 2022,
            
            /// <summary>
            /// if this is selected give an information that it is currently not supported
            /// </summary>
            NotSupported = 0
        };

        /// <summary>
        /// get revit version
        /// </summary>
        /// <returns>integer value of version number</returns>
        public static rvtVersion GetVersionInfo(Autodesk.Revit.ApplicationServices.Application app)
        {
            int num = int.Parse(app.VersionNumber);

            rvtVersion rV = 0;

            switch (num)
            {
                case 2020:
                    rV = rvtVersion.R20;
                    break;
                case 2021:
                    rV = rvtVersion.R21;
                    break;
                case 2022:
                    rV = rvtVersion.R22;
                    break;
                default:
                    rV = 0;
                    break;
            }

            return rV;
        }


    }

    public static class IfcGuid
    {
        #region Static Fields

        /// <summary>
        ///     The replacement table
        /// </summary>
        private static readonly char[] Base64Chars =
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
                'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '_', '$'
            };

        #endregion

        /// <summary>
        /// The reverse function to calculate the number from the characters
        /// </summary>
        /// <param name="str">
        /// The char array to convert from
        /// </param>
        /// <param name="start">
        /// Position in array to start read
        /// </param>
        /// <param name="len">
        /// The length to read
        /// </param>
        /// <returns>
        /// The calculated nuber
        /// </returns>
        public static uint CvFrom64(char[] str, int start, int len)
        {
            int i;
            uint res = 0;

            Debug.Assert(len <= 4, "Length must be equal or lett than 4");

            for (i = 0; i < len; i++)
            {
                int index = -1;
                int j;
                for (j = 0; j < 64; j++)
                {
                    if (Base64Chars[j] == str[start + i])
                    {
                        index = j;
                        break;
                    }
                }

                Debug.Assert(index >= 0, "Index is less than 0");

                res = (res * 64) + ((uint)index);
            }

            return res;
        }

        /// <summary>
        /// Conversion of an integer into a characters with base 64
        ///     using the table Base64Chars
        /// </summary>
        /// <param name="number">
        /// The number to convert
        /// </param>
        /// <param name="result">
        /// The result char array to write to
        /// </param>
        /// <param name="start">
        /// The position in the char array to start writing
        /// </param>
        /// <param name="len">
        /// The length to write
        /// </param>
        public static void CvTo64(uint number, ref char[] result, int start, int len)
        {
            int digit;

            Debug.Assert(len <= 4, "Length must be equal or lett than 4");

            uint act = number;
            int digits = len;

            for (digit = 0; digit < digits; digit++)
            {
                result[start + len - digit - 1] = Base64Chars[(int)(act % 64)];
                act /= 64;
            }

            Debug.Assert(act == 0, "Logic failed, act was not null: " + act);
        }

        /// <summary>
        /// Reconstruction of the GUID from an IFC GUID string (base64)
        /// </summary>
        /// <param name="guid">
        /// The GUID string to convert. Must be 22 characters long
        /// </param>
        /// <returns>
        /// GUID correspondig to the string
        /// </returns>
        public static Guid FromIfcGuid(string guid)
        {
            Debug.Assert(guid.Length == 22, "Input string must not be longer that 22 chars");
            var num = new uint[6];
            char[] str = guid.ToCharArray();
            int n = 2, pos = 0, i;
            for (i = 0; i < 6; i++)
            {
                num[i] = CvFrom64(str, pos, n);
                pos += n;
                n = 4;
            }

            var a = (int)((num[0] * 16777216) + num[1]);
            var b = (short)(num[2] / 256);
            var c = (short)(((num[2] % 256) * 256) + (num[3] / 65536));
            var d = new byte[8];
            d[0] = Convert.ToByte((num[3] / 256) % 256);
            d[1] = Convert.ToByte(num[3] % 256);
            d[2] = Convert.ToByte(num[4] / 65536);
            d[3] = Convert.ToByte((num[4] / 256) % 256);
            d[4] = Convert.ToByte(num[4] % 256);
            d[5] = Convert.ToByte(num[5] / 65536);
            d[6] = Convert.ToByte((num[5] / 256) % 256);
            d[7] = Convert.ToByte(num[5] % 256);

            return new Guid(a, b, c, d);
        }

        /// <summary>
        /// Conversion of a GUID to a string representing the GUID
        /// </summary>
        /// <param name="guid">
        /// The GUID to convert
        /// </param>
        /// <returns>
        /// IFC (base64) encoded GUID string
        /// </returns>
        public static string ToIfcGuid(Guid guid)
        {
            var num = new uint[6];
            var str = new char[22];
            byte[] b = guid.ToByteArray();

            // Creation of six 32 Bit integers from the components of the GUID structure
            num[0] = BitConverter.ToUInt32(b, 0) / 16777216;
            num[1] = BitConverter.ToUInt32(b, 0) % 16777216;
            num[2] = (uint)((BitConverter.ToUInt16(b, 4) * 256) + (BitConverter.ToUInt16(b, 6) / 256));
            num[3] = (uint)(((BitConverter.ToUInt16(b, 6) % 256) * 65536) + (b[8] * 256) + b[9]);
            num[4] = (uint)((b[10] * 65536) + (b[11] * 256) + b[12]);
            num[5] = (uint)((b[13] * 65536) + (b[14] * 256) + b[15]);

            // Conversion of the numbers into a system using a base of 64
            int n = 2;
            int pos = 0;
            for (int i = 0; i < 6; i++)
            {
                CvTo64(num[i], ref str, pos, n);
                pos += n;
                n = 4;
            }

            return new string(str);
        }
    }
}
