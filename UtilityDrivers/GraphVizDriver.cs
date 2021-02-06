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
    public class GraphVizDriver
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Snowflake.GrantReport.Console");

        #region Public properties

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

        public bool ValidateToolInstalled(ProgramOptions programOptions)
        {
            string dotVersionOutput = String.Empty;
            string dotVersionOutputError = String.Empty;

            try
            {
                // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
                // {
                //     Environment.CurrentDirectory = Path.Combine(programOptions.ProgramLocationFolderPath, "graphviz", "win");
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == true || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
                // {
                //     // On mac and linux, it needs to be in path
                // }

                // Shipping entire GraphViz library with tool, lets shift into that folder
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "dot";
                    process.StartInfo.Arguments = "-V"; 
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    
                    logger.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    loggerConsole.Trace(String.Format("\"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments));
                    
                    process.Start();

                    this.ExecutableFilePath = "dot"; //Path.Combine(Environment.CurrentDirectory, "dot.exe");

                    dotVersionOutput = process.StandardOutput.ReadToEnd();
                    dotVersionOutputError = process.StandardError.ReadToEnd();

                    logger.Trace("Getting dot version returned {0}", dotVersionOutput);
                    logger.Trace("Getting dot version error returned {0}", dotVersionOutputError);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                loggerConsole.Error("dot did not return version information. Is GraphViz installed and in path?");

                return false;
            }

            Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?)", RegexOptions.IgnoreCase);
            Match match = regexVersion.Match(dotVersionOutputError);
            if (match != null)
            {
                if (match.Groups.Count > 1)
                {
                    this.ExecutableVersion = match.Groups[1].Value;
                }
            }
            
            if (this.ExecutableVersion.Length == 0)
            {
                loggerConsole.Error(String.Format("Unable to parse version information from {0}. Is GraphViz properly installed?", dotVersionOutput));

                return false;
            }
            
            logger.Trace("GraphViz dot is installed in {0}, version {1}", this.ExecutableFilePath, this.ExecutableFullVersion.ToString());
            loggerConsole.Info("GraphViz dot is installed in {0}, version {1}", this.ExecutableFilePath, this.ExecutableFullVersion.ToString());

            Environment.CurrentDirectory = programOptions.ProgramLocationFolderPath;

            return true;
        }

        public bool ConvertGraphVizToFile(string inputGraphVizFilePath, string outputFilePath, string outputFormat)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            logger.Trace(String.Format("{0}=>{1}", inputGraphVizFilePath, outputFormat));

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = this.ExecutableFilePath;
                    // https://graphviz.org/doc/info/command.html
                    process.StartInfo.Arguments = String.Format("-T{0} -o\"{1}\" \"{2}\"", outputFormat, outputFilePath, inputGraphVizFilePath); 
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
                logger.Trace("Converting {0} to {1} took {2:c} ({3} ms)", inputGraphVizFilePath, outputFormat, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("Converting {0} to {1} took {2:c} ({3} ms)", inputGraphVizFilePath, outputFormat, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
            }
        }
    }
}