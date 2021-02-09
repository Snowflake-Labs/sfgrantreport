using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Database : SnowflakeObjectBase
    {        
        public Account Account { get; set; }

        public List<Schema> Schemas { get; set; } = new List<Schema>();

        public Dictionary<string, Schema> SchemasDict { get; set; } = new Dictionary<string, Schema>(StringComparer.InvariantCulture);

        public override string EntityType
        {
            get
            {
                return "DATABASE";
            }
        }
    }
}
