using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class RoleMemberGrantsToUsersMap : ClassMap<RoleMember>
    {
        public RoleMemberGrantsToUsersMap()
        {
            int i = 0;

            Map(m => m.CreatedOn).Name("CREATED_ON").Index(i); i++;
            Map(m => m.DeletedOn).Name("DELETED_ON").Index(i); i++;
            Map(m => m.Name).Name("ROLE").Index(i); i++;
            Map(m => m.ObjectType).Name("GRANTED_TO").Index(i); i++;
            Map(m => m.GrantedTo).Name("GRANTEE_NAME").Index(i); i++;
            Map(m => m.GrantedBy).Name("GRANTED_BY").Index(i); i++;
        }
    }
}