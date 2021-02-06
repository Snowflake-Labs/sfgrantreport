using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Table : SnowflakeObjectBase
    {        
        public Schema Schema { get; set; }

        public override string EntityType
        {
            get
            {
                return "TABLE";
            }
        }
    }
}