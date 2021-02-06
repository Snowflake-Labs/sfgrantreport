using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class GrantMap : ClassMap<Grant>
    {
        public GrantMap()
        {
            int i = 0;

            Map(m => m.Privilege).Index(i); i++;
            Map(m => m.ObjectType).Index(i); i++;
            Map(m => m.ObjectName).Index(i); i++;
            Map(m => m.GrantedTo).Index(i); i++;

            Map(m => m.DBName).Index(i); i++;
            Map(m => m.SchemaName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;

            Map(m => m.GrantedBy).Index(i); i++;
            Map(m => m.WithGrantOption).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTC), i); i++;
        }
    }
}