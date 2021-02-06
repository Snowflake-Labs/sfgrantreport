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

        public override string EntityType
        {
            get
            {
                return "DATABASE";
            }
        }
    }
}
