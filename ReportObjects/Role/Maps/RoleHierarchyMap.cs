using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleHierarchyMap : ClassMap<RoleHierarchy>
    {
        public RoleHierarchyMap()
        {
            int i = 0;
            
            Map(m => m.Name).Index(i); i++;
            Map(m => m.GrantedTo).Index(i); i++;

            Map(m => m.DirectAncestry).Index(i); i++;
            Map(m => m.AncestryPaths).Index(i); i++;
            Map(m => m.NumAncestryPaths).Index(i); i++;

            Map(m => m.ImportantAncestor).Index(i); i++;

            Map(m => m.Type).Index(i); i++;
        }
    }
}