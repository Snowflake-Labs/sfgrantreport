using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleMember
    {
        public DateTime CreatedOn { get; set; }

        public DateTime CreatedOnUTC
        {
            get
            {
                return this.CreatedOn.ToUniversalTime();
            }
            set
            {
                // Do nothing
            }
        }

        public string Name { get; set; }

        public string GrantedBy { get; set; }

        public string GrantedTo { get; set; }

        public string ObjectType { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RoleMember: {0} is granted to {1} [{2}] by {4}",
                this.Name,
                this.GrantedTo,
                this.ObjectType,
                this.GrantedBy);
        }
    }
}
