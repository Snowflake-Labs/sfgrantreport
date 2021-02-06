using NLog;
using System.Collections.Generic;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class JobStepRouter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Snowflake.GrantReport.Console");

        public enum JobStatus
        {
            // Extract steps
            ExtractCurrentContext = 100,
            ExtractUsersRolesGrants = 101,
        
            // Index steps
            IndexUserDetails = 200,
            IndexGrantDetails = 201,
            IndexRoleAndGrantHierarchy = 202,

            // Report steps
            ReportUserRoleGrantsTable = 300,
            ReportUserRoleGrantHierarchyGraphViz = 301,

            // The rest
            Done = 500,
            Error = -1
        }

        private static List<JobStatus> jobSteps = new List<JobStatus>
            {
                // Extract data
                JobStatus.ExtractCurrentContext,
                JobStatus.ExtractUsersRolesGrants,

                // Index data
                JobStatus.IndexUserDetails,
                JobStatus.IndexGrantDetails,
                JobStatus.IndexRoleAndGrantHierarchy,

                // Report data
                JobStatus.ReportUserRoleGrantsTable,
                JobStatus.ReportUserRoleGrantHierarchyGraphViz,

                // Done 
                JobStatus.Done,
                JobStatus.Error
            };
        private static LinkedList<JobStatus> jobStepsLinked = new LinkedList<JobStatus>(jobSteps);

        public static void ExecuteJobThroughSteps(ProgramOptions programOptions)
        {
            // Read job file from the location

            logger.Info("Starting job from status {0}({0:d})", programOptions.ReportJob.Status);
            loggerConsole.Info("Starting job from status {0}({0:d})", programOptions.ReportJob.Status);

            // Run the step and move to next until things are done
            while (programOptions.ReportJob.Status != JobStatus.Done && programOptions.ReportJob.Status != JobStatus.Error)
            {
                logger.Info("Executing job step {0}({0:d})", programOptions.ReportJob.Status);
                loggerConsole.Info("Executing job step {0}({0:d})", programOptions.ReportJob.Status);

                JobStepBase jobStep = getJobStepFromFactory(programOptions.ReportJob.Status);
                if (jobStep != null)
                {
                    if (jobStep.Execute(programOptions) == false)
                    {
                        programOptions.ReportJob.Status = JobStatus.Error;
                    }
                }
                if (programOptions.ReportJob.Status != JobStatus.Error)
                {
                    programOptions.ReportJob.Status = jobStepsLinked.Find(programOptions.ReportJob.Status).Next.Value;
                }

                // Save progress of the report in case we want to resume or rerun later
                FileIOHelper.WriteReportJobToFile(programOptions.ReportJob, programOptions.ReportJobFilePath);
            }

            return;
        }

        private static JobStepBase getJobStepFromFactory(JobStatus jobStatus)
        {
            switch (jobStatus)
            {
                // Extract Data
                case JobStatus.ExtractCurrentContext:
                    return new ExtractCurrentContext();
                case JobStatus.ExtractUsersRolesGrants:
                    return new ExtractUsersRolesGrants();

                // Index data
                case JobStatus.IndexUserDetails:
                    return new IndexUserDetails();
                case JobStatus.IndexGrantDetails:
                    return new IndexGrantDetails();
                case JobStatus.IndexRoleAndGrantHierarchy:
                    return new IndexRoleAndGrantHierarchy();

                // Report data
                case JobStatus.ReportUserRoleGrantsTable:
                    return new ReportUserRoleGrantsTable();
                case JobStatus.ReportUserRoleGrantHierarchyGraphViz:
                    return new ReportUserRoleGrantHierarchyGraphViz();

                default:
                    break;
            }

            return null;
        }
    }
}