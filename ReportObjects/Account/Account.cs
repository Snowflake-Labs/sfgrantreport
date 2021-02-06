using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Account : SnowflakeObjectBase
    {        
        public List<Database> Databases { get; set; } = new List<Database>();

        public override string EntityType
        {
            get
            {
                return "ACCOUNT";
            }
        }
    }
}
