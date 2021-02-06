using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class StepTimingReportMap : ClassMap<StepTiming>
    {
        public StepTimingReportMap()
        {
            int i = 0;
            Map(m => m.StepName).Index(i); i++;
            Map(m => m.StepID).Index(i); i++;

            Map(m => m.Connection).Index(i); i++;
            Map(m => m.Account).Index(i); i++;
            Map(m => m.NumEntities).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.StartTime), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.EndTime), i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.DurationMS).Index(i); i++;

            Map(m => m.JobFileName).Index(i); i++;
        }
    }
}