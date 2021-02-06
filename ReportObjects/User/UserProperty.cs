using CsvHelper.Configuration.Attributes;
using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{
    /// <summary>
    /// Encapsulates DESCRIBE USER output
    /// </summary>
    public class UserProperty
    {
        public string PropName { get; set; }
           
        public string PropValue { get; set; }
        
        public bool IsDefault { get; set; }
        
        public string Description { get; set; }        

        public override String ToString()
        {
            return String.Format(
                "UserProperty: {0}={1}, IsDefault {2}",
                this.PropName,
                this.PropValue,
                this.IsDefault);
        }
    }
}
