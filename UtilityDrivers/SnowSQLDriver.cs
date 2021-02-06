using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Snowflake.GrantReport
{
    public class SnowSQLDriver
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Snowflake.GrantReport.Console");

        #region Public properties

        public string Connection { get; set; }

        public string ExecutableVersion { get; set; } = "";

        public Version ExecutableFullVersion
        {
            get 
            {
                return new Version(this.ExecutableVersion);
            }
        }

        public string ExecutableFilePath { get; set; }
    
        #endregion

        public SnowSQLDriver(string connectionName)
        {
            this.Connection = connectionName;
        }

        public bool ValidateToolInstalled()
        {
            string snowSQLVersionOutput = String.Empty;
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "snowsql";
                    process.StartInfo.Arguments = "--version"; 
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    
                    logger.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    loggerConsole.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    
                    process.Start();

                    // Wait for the process to start
                    Thread.Sleep(500);
                    this.ExecutableFilePath = process.MainModule.FileName;

                    snowSQLVersionOutput = process.StandardOutput.ReadToEnd();
                    string snowSQLVersionOutputError = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    logger.Trace("Getting SnowSQL version returned {0}", snowSQLVersionOutput);
                    logger.Trace("Getting SnowSQL version error returned {0}", snowSQLVersionOutputError);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                loggerConsole.Error("SnowSQL did not return version information. Is SnowSQL installed and in path?");

                return false;
            }

            Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?)", RegexOptions.IgnoreCase);
            Match match = regexVersion.Match(snowSQLVersionOutput);
            if (match != null)
            {
                if (match.Groups.Count > 1)
                {
                    this.ExecutableVersion = match.Groups[1].Value;
                }
            }
            
            if (this.ExecutableVersion.Length == 0)
            {
                loggerConsole.Error(String.Format("Unable to parse version information from {0}. Is SnowSQL properly installed?", snowSQLVersionOutput));

                return false;
            }
            
            logger.Trace("SnowSQL is installed in {0}, version {1}", this.ExecutableFilePath, this.ExecutableFullVersion.ToString());
            loggerConsole.Info("SnowSQL is installed in {0}, version {1}", this.ExecutableFilePath, this.ExecutableFullVersion.ToString());

            return true;
        }

        public bool ExecuteSQLStatementsInFile(string sqlFilePath, string outputFolderPath)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            logger.Trace(String.Format("Running {0} using connection {1}, saving to {2}", sqlFilePath, this.Connection, outputFolderPath));

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = this.ExecutableFilePath;
                    process.StartInfo.Arguments = String.Format("--connection {0} --filename \"{1}\" --variable reportfolderpath=\"{2}\"", this.Connection, sqlFilePath, outputFolderPath); 
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    
                    logger.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    loggerConsole.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    
                    process.Start();
                    
                    process.WaitForExit();
                }

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
                logger.Trace("Running {0} using connection {1} took {2:c} ({3} ms)", sqlFilePath, this.Connection, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("Running {0} using connection {1} took {2:c} ({3} ms)", sqlFilePath, this.Connection, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
            }
        }
    }
}