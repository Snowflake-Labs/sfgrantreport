using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleHierarchy
    {                
        public string AncestryPaths { get; set; }

        public string DirectAncestry { get; set; }

        public string GrantedTo { get; set; }

        public string ImportantAncestor { get; set; }

        public string Name { get; set; }
        
        public int NumAncestryPaths { get; set; }


        public RoleType Type { get; set; } = RoleType.Unknown;

        public override String ToString()
        {
            return String.Format(
                "RoleHierarhcy: {0}, granted to {1} users, {2}, {3}",
                this.Name,
                this.GrantedTo, 
                this.Type,
                this.AncestryPaths);
        }
    }
}
