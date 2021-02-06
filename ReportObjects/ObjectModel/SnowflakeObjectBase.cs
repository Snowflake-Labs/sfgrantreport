using System;
using System.Collections.Generic;

namespace Snowflake.GrantReport.ReportObjects
{
    public class SnowflakeObjectBase
    {        
        public string ShortName { get; set; }
        
        public string FullName { get; set; }

        public virtual string EntityType { get; }

        public List<Grant> Grants { get; set; } = new List<Grant>();

        public override String ToString()
        {
            return String.Format(
                "{0}: {1} [{2}] with {3} grants",
                this.GetType().Name,
                this.FullName,
                this.EntityType, 
                this.Grants.Count);
        }        
    }
}
