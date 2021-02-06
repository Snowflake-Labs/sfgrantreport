using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleMap : ClassMap<Role>
    {
        public RoleMap()
        {
            int i = 0;
            
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Owner).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
            Map(m => m.IsInherited).Index(i); i++;

            Map(m => m.NumAssignedUsers).Index(i); i++;
            Map(m => m.NumChildRoles).Index(i); i++;
            Map(m => m.NumParentRoles).Index(i); i++;

            Map(m => m.AssignedUsers).Index(i); i++;
            Map(m => m.ChildRolesString).Name("ChildRoles").Index(i); i++;
            Map(m => m.ParentRolesString).Name("ParentRoles").Index(i); i++;
            Map(m => m.AncestryPaths).Index(i); i++;
            Map(m => m.NumAncestryPaths).Index(i); i++;

            Map(m => m.Comment).Index(i); i++;
            
            Map(m => m.IsObjectIdentifierSpecialCharacters).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTC), i); i++;
        }
    }
}