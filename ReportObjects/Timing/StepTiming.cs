using System;

namespace Snowflake.GrantReport.ReportObjects
{
    public class StepTiming
    {
        public string Connection { get; set; }

        public string Account { get; set; }

        public string JobFileName { get; set; }

        public string StepName { get; set; }
        public int StepID { get; set; }
        public int NumEntities { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long DurationMS { get; set; }

        public override String ToString()
        {
            return String.Format(
                "StepTiming: {0}({1}):{2} {3}/{4}",
                this.StepName,
                this.StepID,
                this.NumEntities,
                this.Duration,
                this.DurationMS);
        }
    }
}
