using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class ExtractCurrentContext : JobStepBase
    {
        public override bool Execute(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StepTiming stepTimingFunction = new StepTiming();
            stepTimingFunction.JobFileName = programOptions.ReportJobFilePath;
            stepTimingFunction.StepName = programOptions.ReportJob.Status.ToString();
            stepTimingFunction.StepID = (int)programOptions.ReportJob.Status;
            stepTimingFunction.StartTime = DateTime.Now;
            stepTimingFunction.NumEntities = 0;

            this.DisplayJobStepStartingStatus(programOptions);

            this.FilePathMap = new FilePathMap(programOptions);

            SnowSQLDriver snowSQLDriver = null;
            try
            {
                snowSQLDriver = new SnowSQLDriver(programOptions.ConnectionName);

                if (snowSQLDriver.ValidateToolInstalled() == false)
                {
                    return false;
                };

                FileIOHelper.CreateFolder(this.FilePathMap.Data_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Data_Connection_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Data_Account_FolderPath());

                StringBuilder sb = new StringBuilder(1024);
                sb.AppendLine("!set output_format=csv");
                sb.AppendLine("!set header=true");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentAccount_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_ACCOUNT() AS CURRENT_ACCOUNT;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentRegion_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_REGION() AS CURRENT_REGION;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentVersion_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_VERSION() AS CURRENT_VERSION;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentClient_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_CLIENT() AS CURRENT_VERSION;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentUser_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_USER() AS CURRENT_USER;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentRole_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_ROLE() AS CURRENT_ROLE;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentWarehouse_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_WAREHOUSE() AS CURRENT_WAREHOUSE;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentDatabase_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_DATABASE() AS CURRENT_DATABASE;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_CurrentSchema_FilePath()); sb.AppendLine();
                sb.AppendLine("SELECT CURRENT_SCHEMA() AS CURRENT_SCHEMA;");
                sb.AppendLine(@"!spool off");

                FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.Data_CurrentContext_SQLQuery_FilePath(), false);

                loggerConsole.Info("Retrieving current connection context info");
                snowSQLDriver.ExecuteSQLStatementsInFile(this.FilePathMap.Data_CurrentContext_SQLQuery_FilePath(), programOptions.ReportFolderPath);
                
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                this.DisplayJobStepEndedStatus(programOptions, stopWatch);

                stepTimingFunction.EndTime = DateTime.Now;
                stepTimingFunction.Duration = stopWatch.Elapsed;
                stepTimingFunction.DurationMS = stopWatch.ElapsedMilliseconds;

                List<StepTiming> stepTimings = new List<StepTiming>(1);
                stepTimings.Add(stepTimingFunction);
                FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
            }
        }

        public override bool ShouldExecute(ProgramOptions programOptions)
        {
            if (programOptions.ConnectionName != null && programOptions.ConnectionName.Length > 0)
            {
                logger.Trace("Connection name is not empty. Will execute");
                loggerConsole.Trace("Connection name is not empty. Will execute");
                return true;
            }
            else
            {
                logger.Trace("Connection name is empty. Skipping this step");
                loggerConsole.Trace("Connection name is empty. Skipping this step");
                return false;
            }
        }
    }
}