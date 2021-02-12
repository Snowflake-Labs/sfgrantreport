using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class GrantGrantToRolesMap : ClassMap<Grant>
    {
        public GrantGrantToRolesMap()
        {
            int i = 0;

            Map(m => m.CreatedOn).Name("CREATED_ON").Index(i); i++;
            Map(m => m.Privilege).Name("PRIVILEGE").Index(i); i++;
            Map(m => m.ObjectType).Name("GRANTED_ON").Index(i); i++;
            Map(m => m.EntityName).Name("NAME").Index(i); i++;
            Map(m => m.DBName).Name("TABLE_CATALOG").Index(i); i++;
            Map(m => m.SchemaName).Name("TABLE_SCHEMA").Index(i); i++;
            Map(m => m.GrantedTo).Name("GRANTEE_NAME").Index(i); i++;
            Map(m => m.GrantedBy).Name("GRANTED_BY").Index(i); i++;
            Map(m => m.WithGrantOption).Name("GRANT_OPTION").Index(i); i++;
            Map(m => m.DeletedOn).Name("DELETED_ON").Index(i); i++;
        }
    }
}