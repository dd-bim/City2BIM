using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB.ExtensibleStorage;

namespace City2RVT
{
    class utils
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
    }
}
