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

using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class ReportGrantDifferences : JobStepBase
    {
        #region Constants for report contents

        private const string SHEET_DIFFERENCES = "Differences";
        private const string SHEET_DIFFERENCES_TYPE = "Differences.Type";
        private const string TABLE_DIFFERENCES = "t_Differences";
        private const string PIVOT_DIFFERENCES_TYPE = "p_Differences_Type";
        private const string GRAPH_DIFFERENCES_TYPE = "p_Differences_Type";

        private const int LIST_SHEET_START_TABLE_AT = 4;
        private const int PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int PIVOT_SHEET_CHART_HEIGHT = 14;

        // Configuration comparison colors
        // This color is for differences, kind of pinkish
        internal static Color colorDifferent = Color.FromArgb(0xE8, 0xAF, 0xC3);
        // This color is for missing configuration entries
        internal static Color colorMissing = Color.LightBlue;
        // This color is for extra configuration entries. Similar to Color.Orange
        internal static Color colorExtra = Color.FromArgb(0xFF, 0xC0, 0x0);

        #endregion

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

            try
            {
                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("Snowflake Grant Report Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "Snowflake Grant Differences Report";
                excelReport.Workbook.Properties.Subject = String.Format("{0}<->{1}", programOptions.LeftReportFolderPath, programOptions.RightReportFolderPath);
                excelReport.Workbook.Properties.Comments = String.Format("Command line {0}", programOptions);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                var objectToRolePermissionCellStyle = sheet.Workbook.Styles.CreateNamedStyle("ShortPermissionStyle");
                objectToRolePermissionCellStyle.Style.Font.Size = 8;

                fillReportParametersSheet(sheet, programOptions, excelReport.Workbook.Properties.Title);

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DIFFERENCES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DIFFERENCES_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DIFFERENCES_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DIFFERENCES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Differences sheet

                sheet = excelReport.Workbook.Worksheets[SHEET_DIFFERENCES];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleGrant_Differences_FilePath(), 0, typeof(GrantDifference), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DIFFERENCES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Privilege"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ObjectType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ObjectName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["Difference"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTCLeft"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTCRight"].Position + 1).Width = 20;
                    
                    ExcelAddress cfAddressDifference = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Difference"].Position + 1, sheet.Dimension.Rows, table.Columns["Difference"].Position + 1);
                    var cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
                    cfUserExperience.Style.Font.Color.Color = Color.Black;
                    cfUserExperience.Style.Fill.BackgroundColor.Color = colorDifferent;
                    cfUserExperience.Formula = String.Format(@"=""{0}""", DIFFERENCE_DIFFERENT);

                    cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
                    cfUserExperience.Style.Font.Color.Color = Color.Black;
                    cfUserExperience.Style.Fill.BackgroundColor.Color = colorExtra;
                    cfUserExperience.Formula = String.Format(@"=""{0}""", DIFFERENCE_EXTRA);

                    cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
                    cfUserExperience.Style.Font.Color.Color = Color.Black;
                    cfUserExperience.Style.Fill.BackgroundColor.Color = colorMissing;
                    cfUserExperience.Formula = String.Format(@"=""{0}""", DIFFERENCE_MISSING);

                    sheet = excelReport.Workbook.Worksheets[SHEET_DIFFERENCES_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_DIFFERENCES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "ObjectType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ObjectName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Privilege", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "GrantedTo", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "DifferenceDetails", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Difference", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "UniqueIdentifier", DataFieldFunctions.Count, "NumDifferences");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_DIFFERENCES_TYPE, eChartType.ColumnStacked, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);                    
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file

                string reportFilePath = FilePathMap.GrantsDifferencesExcelReportFilePath();

                logger.Info("Saving Excel report {0}", reportFilePath);
                loggerConsole.Info("Saving Excel report {0}", reportFilePath);

                try
                {
                    // Save full report Excel files
                    excelReport.SaveAs(new FileInfo(reportFilePath));
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn("Unable to save Excel file {0}", reportFilePath);
                    logger.Warn(ex);
                    loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);
                }

                #endregion

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
            if (programOptions.LeftReportFolderPath != null && programOptions.RightReportFolderPath != null && programOptions.LeftReportFolderPath.Length > 0 && programOptions.RightReportFolderPath.Length > 0)
            {
                logger.Trace("Left and Right folder path is not empty. Will execute");
                loggerConsole.Trace("Left and Right folder path not empty. Will execute");
                return true;
            }
            else
            {
                logger.Trace("Left and Right folder path is empty. Skipping this step");
                loggerConsole.Trace("Left and Right folder path is empty. Skipping this step");
                return false;
            }
        }
    }
}