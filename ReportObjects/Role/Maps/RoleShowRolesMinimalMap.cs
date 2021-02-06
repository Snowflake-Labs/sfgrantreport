using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleShowRolesMinimalMap : ClassMap<Role>
    {
        public RoleShowRolesMinimalMap()
        {
            int i = 0;
            
            Map(m => m.Name).Name("name").Index(i); i++;
        }
    }
}