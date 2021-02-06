using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleMemberShowGrantsMap : ClassMap<RoleMember>
    {
        public RoleMemberShowGrantsMap()
        {
            int i = 0;

            Map(m => m.CreatedOn).Name("created_on").Index(i); i++;
            Map(m => m.Name).Name("role").Index(i); i++;
            Map(m => m.ObjectType).Name("granted_to").Index(i); i++;
            Map(m => m.GrantedTo).Name("grantee_name").Index(i); i++;
            Map(m => m.GrantedBy).Name("granted_by").Index(i); i++;
        }
    }
}