using CommandLine;
using Newtonsoft.Json;
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

            if (programOptions.InputFolderPath.Length > 0)
            {
                programOptions.InputFolderPath = Path.GetFullPath(programOptions.InputFolderPath);
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

            // Run report generation
            JobStepRouter.ExecuteJobThroughSteps(programOptions);
        }
    }
}
