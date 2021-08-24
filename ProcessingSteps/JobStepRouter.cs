// Copyright (c) 2021 Snowflake Inc. All rights reserved.

// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using NLog;
using System;
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
            IndexGrantDetailsAccountUsage = 202,
            IndexRoleAndGrantHierarchy = 203,
            IndexGrantDifferences = 204,

            // Report steps
            ReportUserRoleGrantsTable = 300,
            ReportUserRoleGrantHierarchyGraphViz = 301,
            ReportGrantDifferences = 302,

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
                JobStatus.IndexGrantDetailsAccountUsage,
                JobStatus.IndexRoleAndGrantHierarchy,
                JobStatus.IndexGrantDifferences,

                // Report data
                JobStatus.ReportUserRoleGrantsTable,
                JobStatus.ReportUserRoleGrantHierarchyGraphViz,
                JobStatus.ReportGrantDifferences,

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
                    if (jobStep.ShouldExecute(programOptions) == true)
                    {
                        if (jobStep.Execute(programOptions) == false)
                        {
                            programOptions.ReportJob.Status = JobStatus.Error;
                        }
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
                case JobStatus.IndexGrantDetailsAccountUsage:
                    return new IndexGrantDetailsAccountUsage();
                case JobStatus.IndexRoleAndGrantHierarchy:
                    return new IndexRoleAndGrantHierarchy();
                case JobStatus.IndexGrantDifferences:
                    return new IndexGrantDifferences();

                // Report data
                case JobStatus.ReportUserRoleGrantsTable:
                    return new ReportUserRoleGrantsTable();
                case JobStatus.ReportUserRoleGrantHierarchyGraphViz:
                    return new ReportUserRoleGrantHierarchyGraphViz();
                case JobStatus.ReportGrantDifferences:
                    return new ReportGrantDifferences();

                default:
                    break;
            }

            return null;
        }
    }
}