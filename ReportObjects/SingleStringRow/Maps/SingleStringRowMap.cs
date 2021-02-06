using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class SingleStringRowMap : ClassMap<SingleStringRow>
    {
        public SingleStringRowMap()
        {
            int i = 0;

            Map(m => m.Value).Index(i); i++;
        }
    }
}