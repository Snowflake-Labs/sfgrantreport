using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{
    public class SingleStringRow
    {
        public string Value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ObjectType: {0}",
                this.Value);
        }
    }
}
