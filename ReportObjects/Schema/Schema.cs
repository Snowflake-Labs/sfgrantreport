using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Schema : SnowflakeObjectBase
    {        
        public Database Database { get; set; }

        public List<Table> Tables { get; set; } = new List<Table>();
        
        public List<View> Views { get; set; } = new List<View>();

        public override string EntityType
        {
            get
            {
                return "SCHEMA";
            }
        }
    }
}