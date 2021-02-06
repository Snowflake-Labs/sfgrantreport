using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleMemberMap : ClassMap<RoleMember>
    {
        public RoleMemberMap()
        {
            int i = 0;

            Map(m => m.Name).Index(i); i++;
            Map(m => m.ObjectType).Index(i); i++;
            Map(m => m.GrantedTo).Index(i); i++;
            Map(m => m.GrantedBy).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTC), i); i++;
        }
    }
}