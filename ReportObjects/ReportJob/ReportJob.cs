using Newtonsoft.Json;
using Snowflake.GrantReport.ProcessingSteps;
using System;

namespace Snowflake.GrantReport
{
    public class ReportJob
    {
        public string Connection { get; set; }

        public DateTime DataRetrievedOn { get; set; }

        public DateTime DataRetrievedOnUtc { get; set; }

        public JobStepRouter.JobStatus Status { get; set; }

        public string Version { get; set; }
        
        [JsonIgnore]
        public Version VersionFull
        {
            get 
            {
                return new Version(this.Version);
            }
        }
    }
}