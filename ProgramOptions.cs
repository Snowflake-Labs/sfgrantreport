using CommandLine;
using System;

namespace Snowflake.GrantReport
{
    public class ProgramOptions
    {
        [Option('c', "connection", Required = true, HelpText = "Name of the SnowSQL connection entry that will be used to connect to Snowflake.", SetName = "extract")]
        public string ConnectionName { get; set; }

        [Option('i', "input-folder", Required = true, HelpText = "Folder where the files from ACCOUNT_USAGE share in SNOWFLAKE database are stored.", SetName = "accountusage")]
        public string InputFolderPath { get; set; }

        [Option('o', "output-folder", Required = false, HelpText = "Output folder where to create report.")]
        public string ReportFolderPath { get; set; }

        [Option('d', "delete-previous-report-output", Required = false, HelpText = "If true, delete any results of previous processing.")]
        public bool DeletePreviousReportOutput { get; set; }

        [Option('s', "sequential-processing", Required = false, HelpText = "If true, process certain items during extraction and conversion sequentially.")]
        public bool ProcessSequentially { get; set; }

        public string ReportJobFilePath { get; set; }
        public ReportJob ReportJob { get; set; }
        public string ProgramLocationFolderPath { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ProgramOptions: ConnectionName='{0}' OutputFolderPath='{1}' DeletePreviousReportOutput='{2}' InputFolderPath='{3}' ProcessSequentially='{4}'",
                this.ConnectionName, 
                this.ReportFolderPath, 
                this.DeletePreviousReportOutput,
                this.InputFolderPath,
                this.ProcessSequentially);
        }
    }

}