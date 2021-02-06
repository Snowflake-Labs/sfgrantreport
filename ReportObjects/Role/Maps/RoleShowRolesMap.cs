using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleShowRolesMap : ClassMap<Role>
    {
        public RoleShowRolesMap()
        {
            int i = 0;
            
            Map(m => m.Name).Name("name").Index(i); i++;
            Map(m => m.Comment).Name("comment").Index(i); i++;
            Map(m => m.Owner).Name("owner").Index(i); i++;

            Map(m => m.IsInheritedRaw).Name("is_inherited").Index(i); i++;

            Map(m => m.NumAssignedUsers).Name("assigned_to_users").Index(i); i++;
            Map(m => m.NumChildRoles).Name("granted_roles").Index(i); i++;
            Map(m => m.NumParentRoles).Name("granted_to_roles").Index(i); i++;

            Map(m => m.CreatedOn).Name("created_on").Index(i); i++;
        }
    }
}