using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class UserShowUsersMap : ClassMap<User>
    {
        public UserShowUsersMap()
        {
            int i = 0;
            
            Map(m => m.NAME).Name("name").Index(i); i++;
            Map(m => m.LOGIN_NAME).Name("login_name").Index(i); i++;
            
            Map(m => m.CreatedOn).Name("created_on").Index(i); i++;
            Map(m => m.Owner).Name("owner").Index(i); i++;
            Map(m => m.LastLogon).Name("last_success_login").Index(i); i++;
        }
    }
}