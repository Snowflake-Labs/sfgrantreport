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

        [Option('l', "left-folder-compare", Required = true, HelpText = "Left folder containing report files to compare against.", SetName = "compare")]
        public string LeftReportFolderPath { get; set; }
        
        [Option('r', "right-folder-compare", Required = true, HelpText = "Right folder containing report files to compare with.", SetName = "compare")]
        public string RightReportFolderPath { get; set; }
        
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
                "ProgramOptions: ConnectionName='{0}' OutputFolderPath='{1}' DeletePreviousReportOutput='{2}' InputFolderPath='{3}' ProcessSequentially='{4}' LeftReportFolderPath='{4}' RightReportFolderPath='{4}'",
                this.ConnectionName, 
                this.ReportFolderPath, 
                this.DeletePreviousReportOutput,
                this.InputFolderPath,
                this.ProcessSequentially,
                this.LeftReportFolderPath,
                this.RightReportFolderPath);
        }
    }

}