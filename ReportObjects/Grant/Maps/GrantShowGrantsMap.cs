using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class GrantShowGrantsMap : ClassMap<Grant>
    {
        public GrantShowGrantsMap()
        {
            int i = 0;

            Map(m => m.CreatedOn).Name("created_on").Index(i); i++;
            Map(m => m.Privilege).Name("privilege").Index(i); i++;
            Map(m => m.ObjectType).Name("granted_on").Index(i); i++;
            Map(m => m.ObjectName).Name("name").Index(i); i++;
            Map(m => m.GrantedTo).Name("grantee_name").Index(i); i++;
            Map(m => m.GrantedBy).Name("granted_by").Index(i); i++;
            Map(m => m.WithGrantOption).Name("grant_option").Index(i); i++;
        }
    }
}