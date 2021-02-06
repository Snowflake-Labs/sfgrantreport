using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class UserShowUsersMinimalMap : ClassMap<User>
    {
        public UserShowUsersMinimalMap()
        {
            int i = 0;
            
            Map(m => m.NAME).Name("name").Index(i); i++;
            Map(m => m.LOGIN_NAME).Name("login_name").Index(i); i++;
        }
    }
}