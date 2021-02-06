using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleOwnerSankeyMap : ClassMap<Role>
    {
        public RoleOwnerSankeyMap()
        {
            int i = 0;
            
            Map(m => m.Owner).Index(i); i++;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.NumAssignedUsers).Constant(1); i++;
            Map().Constant("orange"); i++;
        }
    }
}