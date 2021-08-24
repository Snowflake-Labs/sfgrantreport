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

using CommandLine;
using NLog;
using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Snowflake.GrantReport.ProcessingSteps;

namespace Snowflake.GrantReport
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Snowflake.GrantReport.Console");

        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                // Some debug information about the environment
                logger.Trace("Snowflake Grant Report Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                loggerConsole.Info("Snowflake Grant Report Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                logger.Trace("Starting at local {0:o}/UTC {1:o}, Parameters={2}", DateTime.Now, DateTime.UtcNow, String.Join(" ", args));
                logger.Trace("Timezone {0} {1} {2}", TimeZoneInfo.Local.DisplayName, TimeZoneInfo.Local.StandardName, TimeZoneInfo.Local.BaseUtcOffset);
                logger.Trace("Culture {0}({1}), ShortDate={2}, ShortTime={3}, All={4}", CultureInfo.CurrentCulture.DisplayName, CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern, String.Join(";", CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns()));
                logger.Trace("Framework {0}", RuntimeInformation.FrameworkDescription);
                logger.Trace("OS Architecture {0}", RuntimeInformation.OSArchitecture);
                logger.Trace("OS {0}", RuntimeInformation.OSDescription);
                logger.Trace("Process Architecture {0}", RuntimeInformation.ProcessArchitecture);
                logger.Trace("Number of Processors {0}", Environment.ProcessorCount);
                logger.Trace("Hostname {0}", Environment.MachineName);
                logger.Trace("Username {0}", Environment.UserName);

                var parserResult = Parser.Default
                    .ParseArguments<ProgramOptions>(args)
                    .WithParsed((ProgramOptions programOptions) => { RunProgram(programOptions); })
                    .WithNotParsed((errs) =>
                    {
                        logger.Error("Could not parse command line arguments into ProgramOptions");
                        //loggerConsole.Error("Could not parse command line arguments into ProgramOptions");
                    });
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);

                // Flush all the logs before shutting down
                LogManager.Flush();
            }        
        }

        public static void RunProgram(ProgramOptions programOptions)
        {
            // If output folder isn't specified, set default output folder to be current folder
            if (programOptions.ReportFolderPath == null || programOptions.ReportFolderPath.Length == 0)
            {
                programOptions.ReportFolderPath = Directory.GetCurrentDirectory();
            }
            else
            {
                programOptions.ReportFolderPath = Path.GetFullPath(programOptions.ReportFolderPath);
            }

            logger.Trace("Executing {0}", programOptions);
            loggerConsole.Trace("Executing {0}", programOptions);

            // Set up the output job folder and job file path
            programOptions.ReportJobFilePath = Path.Combine(programOptions.ReportFolderPath, "Snowflake.GrantReport.json");
            programOptions.ProgramLocationFolderPath = AppContext.BaseDirectory;

            // Remove previous job if it exists and it was asked for
            if (Directory.Exists(programOptions.ReportFolderPath) == true &&
                File.Exists(programOptions.ReportJobFilePath) == true &&
                programOptions.DeletePreviousReportOutput == true)
            {
                logger.Info("Clearing output folder {0}", programOptions.ReportFolderPath);
                loggerConsole.Info("Clearing output folder {0}", programOptions.ReportFolderPath);

                if (FileIOHelper.DeleteFolder(programOptions.ReportFolderPath) == false)
                {
                    logger.Error("Unable to clear output folder {0}", programOptions.ReportFolderPath);
                    loggerConsole.Error("Unable to clear output folder {0}", programOptions.ReportFolderPath);

                    return;
                }

                // Sleep after deleting to let the file system catch up
                Thread.Sleep(2000);
            } 

            // Input path for the offline files
            if (programOptions.InputFolderPath == null || programOptions.InputFolderPath.Length == 0)
            {
                programOptions.InputFolderPath = String.Empty;
            }
            else
            {
                programOptions.InputFolderPath = Path.GetFullPath(programOptions.InputFolderPath);
            }

            // Comparison - left side
            if (programOptions.LeftReportFolderPath == null || programOptions.LeftReportFolderPath.Length == 0)
            {
                programOptions.LeftReportFolderPath = String.Empty;
            }
            else
            {
                programOptions.LeftReportFolderPath = Path.GetFullPath(programOptions.LeftReportFolderPath);
            }

            // Comparison - right side
            if (programOptions.RightReportFolderPath == null || programOptions.RightReportFolderPath.Length == 0)
            {
                programOptions.RightReportFolderPath = String.Empty;
            }
            else
            {
                programOptions.RightReportFolderPath = Path.GetFullPath(programOptions.RightReportFolderPath);
            }

            // Create Output folder if it doesn't exist
            if (Directory.Exists(programOptions.ReportFolderPath) == false)
            {
                logger.Info("Creating output folder {0}", programOptions.ReportFolderPath);
                loggerConsole.Info("Creating output folder {0}", programOptions.ReportFolderPath);

                if (FileIOHelper.CreateFolder(programOptions.ReportFolderPath) == false) return;
            }

            if (File.Exists(programOptions.ReportJobFilePath) == false)
            {
                logger.Info("New report {0}", programOptions.ReportJobFilePath);
                loggerConsole.Info("New report {0}", programOptions.ReportJobFilePath);

                // Create job file for new report
                ReportJob reportJob = new ReportJob();
                reportJob.Version = Assembly.GetEntryAssembly().GetName().Version.ToString();
                reportJob.Connection = programOptions.ConnectionName;
                reportJob.InputFolder = programOptions.InputFolderPath;
                reportJob.DataRetrievedOnUtc = DateTime.UtcNow;
                reportJob.DataRetrievedOn = reportJob.DataRetrievedOnUtc.ToLocalTime();
                reportJob.Status = JobStepRouter.JobStatus.ExtractCurrentContext;

                programOptions.ReportJob = reportJob;

                FileIOHelper.WriteReportJobToFile(programOptions.ReportJob, programOptions.ReportJobFilePath);
            }
            else 
            {
                logger.Info("Resume report {0}", programOptions.ReportJobFilePath);
                loggerConsole.Info("Resume report {0}", programOptions.ReportJobFilePath);

                ReportJob reportJob = FileIOHelper.ReadReportJobFromFile(programOptions.ReportJobFilePath);
                if (reportJob == null) return;
                
                // Check version of previous report job file
                logger.Trace("Existing report {0} has version {1}", programOptions.ReportJobFilePath, reportJob.Version);
                if (reportJob.VersionFull < Assembly.GetEntryAssembly().GetName().Version)
                {
                    logger.Warn("Existing report {0} was generated with older version {1}, cannot process with this version of the program", programOptions.ReportJobFilePath, reportJob.Version);
                    loggerConsole.Warn("Existing report {0} was generated with older version {1}, cannot process with this version of the program", programOptions.ReportJobFilePath, reportJob.Version);

                    return;
                }
                else if (reportJob.VersionFull > Assembly.GetEntryAssembly().GetName().Version)
                {
                    logger.Warn("Existing report {0} was generated with newer version {1}, cannot process with this version of the program", programOptions.ReportJobFilePath, reportJob.Version);
                    loggerConsole.Warn("Existing report {0} was generated with newer version {1}, cannot process with this version of the program", programOptions.ReportJobFilePath, reportJob.Version);

                    return;
                }
                
                programOptions.ReportJob = reportJob;
            }

            // Check for input files if we're pulling from offline files from Snowhouse
            if (programOptions.InputFolderPath.Length > 0)
            {
                FilePathMap filePathMap = new FilePathMap(programOptions);
                if (File.Exists(filePathMap.Input_RoleShowGrantsToAndOn_FilePath()) == false)
                {
                    logger.Warn("File {0} must exist when loading from offline files", filePathMap.Input_RoleShowGrantsToAndOn_FilePath());
                    loggerConsole.Warn("File {0} must exist when loading from offline files", filePathMap.Input_RoleShowGrantsToAndOn_FilePath());

                    return;
                }
                if (File.Exists(filePathMap.Input_RoleShowGrantsOf_FilePath()) == false)
                {
                    logger.Warn("File {0} must exist when loading from offline files", filePathMap.Input_RoleShowGrantsOf_FilePath());
                    loggerConsole.Warn("File {0} must exist when loading from offline files", filePathMap.Input_RoleShowGrantsOf_FilePath());

                    return;
                }
            }

            // Run report generation
            JobStepRouter.ExecuteJobThroughSteps(programOptions);
        }
    }
}
