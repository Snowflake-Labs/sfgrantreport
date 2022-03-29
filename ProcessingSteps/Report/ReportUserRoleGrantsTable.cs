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
    public class ReportUserRoleGrantsTable : JobStepBase
    {
        #region Constants for report contents

        private const string SHEET_USERS = "Users";
        private const string TABLE_USERS = "t_Users";
        private const string SHEET_USERS_SHOW_USERS = "Users.ShowUsers";
        private const string SHEET_USERS_CREATED_TIMELINE = "Users.CreatedTimeline";
        private const string TABLE_USERS_SHOW_USERS = "t_Users_ShowUsers";
        private const string PIVOT_USERS_CREATED_TIMELINE = "p_Users_CreatedTimeline";
        private const string GRAPH_USERS_CREATED_TIMELINE = "g_Users_CreatedTimeline";

        private const string SHEET_ROLES = "Roles";
        private const string TABLE_ROLES = "t_Roles";
        private const string SHEET_ROLES_SHOW_ROLES = "Roles.ShowRoles";
        private const string TABLE_ROLES_SHOW_ROLES = "t_Roles_ShowRoles";
        private const string SHEET_ROLES_CREATED_TIMELINE = "Roles.CreatedTimeline";
        private const string PIVOT_ROLES_CREATED_TIMELINE = "p_Roles_CreatedTimeline";
        private const string GRAPH_ROLES_CREATED_TIMELINE = "g_Roles_CreatedTimeline";

        private const string SHEET_ROLES_HIERARCHY = "Roles.Hierarchy";
        private const string TABLE_ROLES_HIERARCHY = "t_RolesHierarchy";

        private const string SHEET_ROLE_MEMBERS = "RoleMembers";
        private const string TABLE_ROLE_MEMBERS = "t_RoleMembers";
        private const string SHEET_ROLE_MEMBERS_SHOW_GRANTS = "RolesMembers.ShowGrants";
        private const string TABLE_ROLE_MEMBERS_SHOW_GRANTS = "t_RoleMemberships_ShowGrants";
        private const string SHEET_ROLE_MEMBERS_CREATED_TIMELINE = "RoleMembers.CreatedTimeline";
        private const string PIVOT_ROLE_MEMBERS_CREATED_TIMELINE = "p_RoleMemberships_CreatedTimeline";
        private const string GRAPH_ROLE_MEMBERS_CREATED_TIMELINE = "g_RoleMemberships_CreatedTimeline";

        private const string SHEET_GRANTS_ALL = "Grants";
        private const string TABLE_GRANTS_ALL = "t_Grants";
        private const string SHEET_GRANTS_TYPE_COUNTS = "Grants.Type.Counts";
        private const string PIVOT_GRANTS_TYPE_COUNTS = "p_Grants_Type_Counts";
        private const string SHEET_GRANTS_TYPE_VS_PRIVILEGE = "Grants.Type.Privilege";
        private const string PIVOT_GRANTS_TYPE_VS_PRIVILEGE = "p_Grants_Type_Privilege";
        private const string SHEET_GRANTS_ROLE_VS_PRIVILEGE = "Grants.Role.Privilege";
        private const string PIVOT_GRANTS_ROLE_VS_PRIVILEGE = "p_Grants_Role_Privilege";
        private const string SHEET_GRANTS_PER_OBJECT_TYPE = "Grants.Lst.{0}";
        private const string TABLE_GRANTS_PER_OBJECT_TYPE = "t_Grants_List_{0}";
        private const string SHEET_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE = "Grants.Pvt.{0}";
        private const string PIVOT_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE = "p_Grants_Type_Privilege_{0}";
        private const string SHEET_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE = "Grants.Tbl.{0}";
        private const string TABLE_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE = "t_Grants_Table_{0}";

        private const string SHEET_GRANTS_IN_DATABASE_FOR_ALL_ROLES = "DB.{0}";
        private const string TABLE_GRANTS_IN_DATABASE_FOR_ALL_ROLES = "t_Grants_DB_{0}";

        private const int LIST_SHEET_START_TABLE_AT = 4;
        private const int PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int PIVOT_SHEET_CHART_HEIGHT = 14;

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
                #region Full report

                List<string> sheetsToIncludeList = new List<string>{SHEET_USERS, SHEET_ROLES, SHEET_ROLE_MEMBERS, SHEET_ROLE_MEMBERS_SHOW_GRANTS, SHEET_GRANTS_ALL, "DatabaseVsRoleVsPermissions"};
List<SingleStringRow> objectTypesList = FileIOHelper.ReadListFromCSVFile<SingleStringRow>(FilePathMap.Report_RoleGrant_ObjectTypes_FilePath(), new SingleStringRowMap());
                if (objectTypesList != null)
                {
                    foreach (SingleStringRow objectType in objectTypesList)
                    {
                        sheetsToIncludeList.Add(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value)));
                    }
                }

                generateExcelReport(programOptions, sheetsToIncludeList, FilePathMap.UsersRolesAndGrantsExcelReportFilePath());

                #endregion

                #region Save smaller files

                sheetsToIncludeList = new List<string>{SHEET_USERS, SHEET_ROLES, SHEET_ROLE_MEMBERS, SHEET_ROLE_MEMBERS_SHOW_GRANTS};
                generateExcelReport(programOptions, sheetsToIncludeList, FilePathMap.UsersRolesAndGrantsExcelReportFilePath("UsersRoles"));

                sheetsToIncludeList = new List<string>{"Grants"};
                generateExcelReport(programOptions, sheetsToIncludeList, FilePathMap.UsersRolesAndGrantsExcelReportFilePath("Grants"));

                if (objectTypesList != null)
                {
                    foreach (SingleStringRow objectType in objectTypesList)
                    {
                        sheetsToIncludeList = new List<string>{getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value))};
                        generateExcelReport(programOptions, sheetsToIncludeList, FilePathMap.UsersRolesAndGrantsExcelReportFilePath(objectType.Value));
                    }
                }

                sheetsToIncludeList = new List<string>{"DatabaseVsRoleVsPermissions"};
                generateExcelReport(programOptions, sheetsToIncludeList, FilePathMap.UsersRolesAndGrantsExcelReportFilePath("DBGRANTS"));

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
            if ((programOptions.ConnectionName != null && programOptions.ConnectionName.Length > 0) || (programOptions.InputFolderPath != null && programOptions.InputFolderPath.Length > 0))
            {
                logger.Trace("Connection name or Input Folder Path is not empty. Will execute");
                loggerConsole.Trace("Connection name or Input Folder Path is not empty. Will execute");
                return true;
            }
            else
            {
                logger.Trace("Connection name or Input Folder Path is empty. Skipping this step");
                loggerConsole.Trace("Connection name or Input Folder Path is empty. Skipping this step");
                return false;
            }
        }

        private void generateExcelReport(ProgramOptions programOptions, List<string> sheetsToIncludeList, string reportFilePath)        
        {
            #region Prepare the report package

            // Prepare package
            ExcelPackage excelReport = new ExcelPackage();
            excelReport.Workbook.Properties.Author = String.Format("Snowflake Grant Report Version {0}", Assembly.GetEntryAssembly().GetName().Version);
            excelReport.Workbook.Properties.Title = "Snowflake User, Role and Grant Report";
            excelReport.Workbook.Properties.Subject = programOptions.ConnectionName;
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

            if (sheetsToIncludeList.Contains(SHEET_USERS) == true)
            {
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_USERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See SHOW USERS";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS_SHOW_USERS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Created Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS_CREATED_TIMELINE);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_USERS_CREATED_TIMELINE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_USERS_SHOW_USERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
            }

            if (sheetsToIncludeList.Contains(SHEET_USERS) == true)
            {
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See SHOW ROLES";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLES_SHOW_ROLES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Created Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLES_CREATED_TIMELINE);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLES_CREATED_TIMELINE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLES_HIERARCHY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLES_SHOW_ROLES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
            }

            if (sheetsToIncludeList.Contains(SHEET_ROLE_MEMBERS) == true)
            {
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLE_MEMBERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See SHOW ROLES";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLE_MEMBERS_SHOW_GRANTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Created Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLE_MEMBERS_CREATED_TIMELINE);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLE_MEMBERS_CREATED_TIMELINE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLE_MEMBERS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ROLE_MEMBERS_SHOW_GRANTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ROLE_MEMBERS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
            }

            if (sheetsToIncludeList.Contains(SHEET_GRANTS_ALL) == true)
            {
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GRANTS_ALL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Types";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_TYPE_COUNTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Type vs Privilege";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_TYPE_VS_PRIVILEGE);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 3].Value = "See Role vs Privilege";
                sheet.Cells[3, 4].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_ROLE_VS_PRIVILEGE);
                sheet.Cells[3, 4].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GRANTS_TYPE_COUNTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_ALL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GRANTS_TYPE_VS_PRIVILEGE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_ALL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GRANTS_ROLE_VS_PRIVILEGE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Converted Data";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GRANTS_ALL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);
            }

            List<SingleStringRow> objectTypesList = FileIOHelper.ReadListFromCSVFile<SingleStringRow>(FilePathMap.Report_RoleGrant_ObjectTypes_FilePath(), new SingleStringRowMap());
            if (objectTypesList != null)
            {
                foreach (SingleStringRow objectType in objectTypesList)
                {
                    if (sheetsToIncludeList.Contains(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value))) == true)
                    {
                        sheet = excelReport.Workbook.Worksheets.Add(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value)));
                        sheet.Cells[1, 1].Value = "Table of Contents";
                        sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                        sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                        // sheet.Cells[2, 1].Value = "See Type vs Privilege";
                        // sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE, objectType.Value)));
                        // sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                        sheet.Cells[2, 1].Value = "See Privilege Table";
                        sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE, objectType.Value)));
                        sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                        sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                        sheet = excelReport.Workbook.Worksheets.Add(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE, objectType.Value)));
                        sheet.Cells[1, 1].Value = "Table of Contents";
                        sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                        sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                        sheet.Cells[2, 1].Value = "See Converted Data";
                        sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value)));
                        sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                        sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                        // sheet = excelReport.Workbook.Worksheets.Add(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE, objectType.Value)));
                        // sheet.Cells[1, 1].Value = "Table of Contents";
                        // sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                        // sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                        // sheet.Cells[2, 1].Value = "See Converted Data";
                        // sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value)));
                        // sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                        // sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);
                    }
                }
            }

            Account account = null;
            Dictionary<string, string> databaseVsRolesGrantsSheetsDict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (sheetsToIncludeList.Contains("DatabaseVsRoleVsPermissions") == true) 
            {
                account = buildObjectHierarchyWithGrants();
                foreach (Database database in account.Databases)
                {
                    string sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_IN_DATABASE_FOR_ALL_ROLES, database.ShortName));
                    bool haveUniqueSheetName = true;
                    if (databaseVsRolesGrantsSheetsDict.ContainsKey(sheetName) == true)
                    {
                        haveUniqueSheetName = false;
                        // Duplicate name, it is when you have several long-named databases with similar names in first 30 characters
                        for (int i = 0; i < 09; i++)
                        {
                            sheetName = String.Format("{0}{1}", sheetName.Substring(0, sheetName.Length - 1), i);
                            if (databaseVsRolesGrantsSheetsDict.ContainsKey(sheetName) == false) 
                            {
                                databaseVsRolesGrantsSheetsDict.Add(sheetName, database.ShortName);

                                haveUniqueSheetName = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        databaseVsRolesGrantsSheetsDict.Add(sheetName, database.ShortName);
                    }

                    if (haveUniqueSheetName == true)
                    {
                        sheet = excelReport.Workbook.Worksheets.Add(sheetName);
                        sheet.Cells[1, 1].Value = "Table of Contents";
                        sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                        sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                        sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 4);
                    }
                }
            }

            #endregion

            #region Report file variables

            ExcelRangeBase range = null;
            ExcelTable table = null;

            #endregion

            #region User Tables and Pivots

            if (sheetsToIncludeList.Contains(SHEET_USERS) == true)
            {
                sheet = excelReport.Workbook.Worksheets[SHEET_USERS];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_UserDetail_FilePath(), 0, typeof(User), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_USERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["NAME"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LOGIN_NAME"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OWNER"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTC"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LastLogon"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LastLogonUTC"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FIRST_NAME"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["LAST_NAME"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DISPLAY_NAME"].Position + 1).Width = 15;

                    sheet = excelReport.Workbook.Worksheets[SHEET_USERS_CREATED_TIMELINE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_USERS_CREATED_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "DEFAULT_ROLE", eSortType.Ascending, true);
                    addFilterFieldToPivot(pivot, "IsSSOEnabled", eSortType.Ascending, true);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["CreatedOn"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Years |eDateGroupBy.Months | eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Owner", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NAME", DataFieldFunctions.Count, "NumUsers");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_USERS_CREATED_TIMELINE, eChartType.ColumnStacked, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_USERS_SHOW_USERS];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Data_ShowUsers_FilePath(), 0, typeof(Object), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_USERS_SHOW_USERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                }
            }

            #endregion

            #region Role Tables and Pivots

            if (sheetsToIncludeList.Contains(SHEET_ROLES) == true)
            {
                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                sheet = excelReport.Workbook.Worksheets[SHEET_ROLES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleDetail_FilePath(), 0, typeof(Role), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ROLES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Name"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Owner"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTC"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_ROLES_CREATED_TIMELINE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_ROLES_CREATED_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Type", eSortType.Ascending, true);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["CreatedOn"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Years |eDateGroupBy.Months | eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Owner", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Name", DataFieldFunctions.Count, "NumRoles");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_ROLES_CREATED_TIMELINE, eChartType.ColumnStacked, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);
                }

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                sheet = excelReport.Workbook.Worksheets[SHEET_ROLES_SHOW_ROLES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Data_ShowRoles_FilePath(), 0, typeof(Object), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ROLES_SHOW_ROLES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                }

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                sheet = excelReport.Workbook.Worksheets[SHEET_ROLES_HIERARCHY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleHierarchy_FilePath(), 0, typeof(RoleHierarchy), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ROLES_HIERARCHY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Name"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AncestryPaths"].Position + 1).Width = 60;
                }
            }

            #endregion

            #region Role Member Tables and Pivots

            if (sheetsToIncludeList.Contains(SHEET_ROLE_MEMBERS) == true)
            {
                sheet = excelReport.Workbook.Worksheets[SHEET_ROLE_MEMBERS];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleMember_FilePath(), 0, typeof(RoleMember), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ROLE_MEMBERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Name"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["GrantedBy"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTC"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_ROLE_MEMBERS_CREATED_TIMELINE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_ROLE_MEMBERS_CREATED_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ObjectType", eSortType.Ascending, true);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["CreatedOn"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Years |eDateGroupBy.Months | eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "GrantedBy", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Name", DataFieldFunctions.Count, "NumMembers");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_ROLE_MEMBERS_CREATED_TIMELINE, eChartType.ColumnStacked, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_ROLE_MEMBERS_SHOW_GRANTS];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Data_RoleShowGrantsOf_FilePath(), 0, typeof(Object), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ROLE_MEMBERS_SHOW_GRANTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                }
            }

            #endregion

            #region Grant Tables and Pivots

            if (sheetsToIncludeList.Contains(SHEET_GRANTS_ALL) == true)
            {
                sheet = excelReport.Workbook.Worksheets[SHEET_GRANTS_ALL];

                logger.Info("{0} Sheet", sheet.Name);
                loggerConsole.Info("{0} Sheet", sheet.Name);
                
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleGrant_FilePath(), 0, typeof(RoleMember), sheet, LIST_SHEET_START_TABLE_AT, 1);

                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_GRANTS_ALL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Privilege"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ObjectType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ObjectName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["GrantedBy"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUTC"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_GRANTS_TYPE_COUNTS];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_GRANTS_TYPE_COUNTS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "GrantedBy", eSortType.Ascending, true);
                    addRowFieldToPivot(pivot, "ObjectType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ObjectName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Privilege", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "GrantedTo", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "ObjectName", DataFieldFunctions.Count, "NumGrants");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_GRANTS_TYPE_VS_PRIVILEGE];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_GRANTS_TYPE_VS_PRIVILEGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "GrantedBy", eSortType.Ascending, true);
                    addFilterFieldToPivot(pivot, "ObjectType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ObjectName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "GrantedTo", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Privilege", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "ObjectName", DataFieldFunctions.Count, "NumGrants");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_GRANTS_ROLE_VS_PRIVILEGE];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_GRANTS_ROLE_VS_PRIVILEGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "GrantedBy", eSortType.Ascending, true);
                    addRowFieldToPivot(pivot, "GrantedTo", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ObjectType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ObjectName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Privilege", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "ObjectName", DataFieldFunctions.Count, "NumGrants");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }
            }

            #endregion

            #region Grant tables by object type Tables and Pivots

            if (objectTypesList != null)
            {
                foreach (SingleStringRow objectType in objectTypesList)
                {
                    if (sheetsToIncludeList.Contains(getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value))) == true)
                    {
                        sheet = excelReport.Workbook.Worksheets[getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_PER_OBJECT_TYPE, objectType.Value))];

                        logger.Info("{0} Sheet", sheet.Name);
                        loggerConsole.Info("{0} Sheet", sheet.Name);
                        
                        EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleGrant_ObjectType_FilePath(objectType.Value), 0, typeof(RoleMember), sheet, LIST_SHEET_START_TABLE_AT, 1);

                        logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                        loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);

                        if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                        {
                            range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                            table = sheet.Tables.Add(range, String.Format(TABLE_GRANTS_PER_OBJECT_TYPE, objectType.Value));
                            table.ShowHeader = true;
                            table.TableStyle = TableStyles.Medium2;
                            table.ShowFilter = true;
                            table.ShowTotal = false;

                            sheet.Column(table.Columns["Privilege"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ObjectType"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ObjectName"].Position + 1).Width = 30;
                            sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 30;
                            sheet.Column(table.Columns["GrantedBy"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["CreatedOnUTC"].Position + 1).Width = 20;

                            // sheet = excelReport.Workbook.Worksheets[getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE, objectType.Value))];
                            // ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, String.Format(PIVOT_GRANTS_TYPE_VS_PRIVILEGE_PER_OBJECT_TYPE, objectType.Value));
                            // setDefaultPivotTableSettings(pivot);
                            // addFilterFieldToPivot(pivot, "GrantedBy", eSortType.Ascending, true);
                            // addRowFieldToPivot(pivot, "ObjectType", eSortType.Ascending);
                            // addRowFieldToPivot(pivot, "ObjectName", eSortType.Ascending);
                            // addRowFieldToPivot(pivot, "GrantedTo", eSortType.Ascending);
                            // addColumnFieldToPivot(pivot, "Privilege", eSortType.Ascending);
                            // addDataFieldToPivot(pivot, "ObjectName", DataFieldFunctions.Count, "NumGrants");

                            // sheet.Column(1).Width = 20;
                            // sheet.Column(2).Width = 20;
                            // sheet.Column(3).Width = 20;
                        }

                        sheet = excelReport.Workbook.Worksheets[getShortenedNameForExcelSheet(String.Format(SHEET_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE, objectType.Value))];

                        logger.Info("{0} Sheet", sheet.Name);
                        loggerConsole.Info("{0} Sheet", sheet.Name);
                        
                        EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.Report_RoleGrant_ObjectType_Pivoted_FilePath(objectType.Value), 0, typeof(ObjectTypeGrant), sheet, LIST_SHEET_START_TABLE_AT, 1);

                        logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                        loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                        if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                        {
                            range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                            table = sheet.Tables.Add(range, String.Format(TABLE_GRANTS_TYPE_VS_PRIVILEGE_TABLE_PER_OBJECT_TYPE , objectType.Value));
                            table.ShowHeader = true;
                            table.TableStyle = TableStyles.Medium2;
                            table.ShowFilter = true;
                            table.ShowTotal = false;

                            sheet.Column(table.Columns["ObjectType"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ObjectName"].Position + 1).Width = 30;
                            sheet.Column(table.Columns["GrantedTo"].Position + 1).Width = 30;

                            // Make the column for permissions headers angled downwards 45 degrees
                            for (int i = 7; i <= table.Columns.Count; i++)
                            {
                                sheet.Cells[LIST_SHEET_START_TABLE_AT, i].Style.TextRotation = 135;
                                sheet.Column(i).Width = 5;
                            }
                        }
                    }
                }
            }

            #endregion

            #region Database vs Roles vs Permissions single page

            // Build the table 
            // Object       | Role 1    | Role 2    | ...   | Role N
            // -----------------------------------------------------
            // DB1          | U, O, CR S| U         |       |  
            //  Schema1     | U, O, CR T|           |       |    
            //   Table1     | U         | U, R      |       | U, CRUD
            // DB2          ...
            //  Schema2     ...
            //   Table2     ...

            if (sheetsToIncludeList.Contains("DatabaseVsRoleVsPermissions") == true) 
            {
                foreach (Database database in account.Databases)
                {
                    Dictionary<string, int> roleToHeaderMapping = new Dictionary<string, int>();

                    string sheetName = String.Empty;

                    foreach (var kvp in databaseVsRolesGrantsSheetsDict)
                    {
                        if (kvp.Value == database.ShortName) 
                        {
                            sheetName = kvp.Key;
                            break;
                        }
                    }
                    if (sheetName.Length == 0) continue;

                    sheet = excelReport.Workbook.Worksheets[sheetName];

                    logger.Info("{0} Sheet", sheet.Name);
                    loggerConsole.Info("{0} Sheet", sheet.Name);

                    sheet.Cells[2, 1].Value = "Database:";
                    sheet.Cells[2, 2].Value = database.FullName;

                    int headerRowIndex = LIST_SHEET_START_TABLE_AT;
                    int roleColumnBeginIndex = 4;
                    int roleColumnMaxIndex = roleColumnBeginIndex;

                    // Header row
                    sheet.Cells[headerRowIndex, 1].Value = "Type";
                    sheet.Cells[headerRowIndex, 2].Value = "Full Name";
                    sheet.Cells[headerRowIndex, 3].Value = "Short Name";
                    
                    int currentRowIndex = headerRowIndex;
                    currentRowIndex++;

                    // Database name                   
                    sheet.Cells[currentRowIndex, 1].Value = database.EntityType;
                    sheet.Cells[currentRowIndex, 2].Value = database.FullName;
                    sheet.Cells[currentRowIndex, 3].Value = database.ShortName;

                    // Grants to Database
                    var grantsByRoleNameGroups = database.Grants.GroupBy(g => g.GrantedTo);
                    foreach (var grantsByRoleNameGroup in grantsByRoleNameGroups)
                    {
                        Grant firstGrant = grantsByRoleNameGroup.First();
                        int thisRoleColumnIndex = 0;
                        if (roleToHeaderMapping.ContainsKey(firstGrant.GrantedTo) == false)
                        {
                            // Add another Role to the header
                            thisRoleColumnIndex = roleColumnMaxIndex;
                            roleToHeaderMapping.Add(firstGrant.GrantedTo, thisRoleColumnIndex);
                            if (thisRoleColumnIndex <= 16384) sheet.Cells[headerRowIndex, thisRoleColumnIndex].Value = firstGrant.GrantedTo;
                            roleColumnMaxIndex++;
                        }
                        else
                        {
                            // Previously seen
                            thisRoleColumnIndex = roleToHeaderMapping[firstGrant.GrantedTo];
                        }
                        if (thisRoleColumnIndex <= 16384) outputGrantstToCell(sheet.Cells[currentRowIndex, thisRoleColumnIndex], grantsByRoleNameGroup.ToList());
                    }

                    currentRowIndex++;

                    // Schemas
                    foreach (Schema schema in database.Schemas)                    
                    {
                        sheet.Cells[currentRowIndex, 1].Value = schema.EntityType;
                        sheet.Cells[currentRowIndex, 2].Value = schema.FullName;
                        sheet.Cells[currentRowIndex, 3].Value = schema.ShortName;

                        grantsByRoleNameGroups = schema.Grants.GroupBy(g => g.GrantedTo);
                        foreach (var grantsByRoleNameGroup in grantsByRoleNameGroups)
                        {
                            Grant firstGrant = grantsByRoleNameGroup.First();
                            int thisRoleColumnIndex = 0;
                            if (roleToHeaderMapping.ContainsKey(firstGrant.GrantedTo) == false)
                            {
                                // Add another Role to the header
                                thisRoleColumnIndex = roleColumnMaxIndex;
                                roleToHeaderMapping.Add(firstGrant.GrantedTo, thisRoleColumnIndex);
                                if (thisRoleColumnIndex <= 16384) sheet.Cells[headerRowIndex, thisRoleColumnIndex].Value = firstGrant.GrantedTo;
                                roleColumnMaxIndex++;
                            }
                            else
                            {
                                // Previously seen
                                thisRoleColumnIndex = roleToHeaderMapping[firstGrant.GrantedTo];
                            }
                            if (thisRoleColumnIndex <= 16384) outputGrantstToCell(sheet.Cells[currentRowIndex, thisRoleColumnIndex], grantsByRoleNameGroup.ToList());
                        }

                        currentRowIndex++;

                        // Tables
                        foreach(Table table1 in schema.Tables)
                        {
                            sheet.Cells[currentRowIndex, 1].Value = table1.EntityType;
                            sheet.Cells[currentRowIndex, 2].Value = table1.FullName;
                            sheet.Cells[currentRowIndex, 3].Value = table1.ShortName;

                            grantsByRoleNameGroups = table1.Grants.GroupBy(g => g.GrantedTo);
                            foreach (var grantsByRoleNameGroup in grantsByRoleNameGroups)
                            {
                                Grant firstGrant = grantsByRoleNameGroup.First();
                                int thisRoleColumnIndex = 0;
                                if (roleToHeaderMapping.ContainsKey(firstGrant.GrantedTo) == false)
                                {
                                    // Add another Role to the header
                                    thisRoleColumnIndex = roleColumnMaxIndex;
                                    roleToHeaderMapping.Add(firstGrant.GrantedTo, thisRoleColumnIndex);
                                    if (thisRoleColumnIndex <= 16384) sheet.Cells[headerRowIndex, thisRoleColumnIndex].Value = firstGrant.GrantedTo;
                                    roleColumnMaxIndex++;
                                }
                                else
                                {
                                    // Previously seen
                                    thisRoleColumnIndex = roleToHeaderMapping[firstGrant.GrantedTo];
                                }
                                if (thisRoleColumnIndex <= 16384) outputGrantstToCell(sheet.Cells[currentRowIndex, thisRoleColumnIndex], grantsByRoleNameGroup.ToList());
                            }

                            sheet.Row(currentRowIndex).OutlineLevel = 1;
                            currentRowIndex++;
                        }

                        // Views
                        foreach(View view in schema.Views)
                        {
                            sheet.Cells[currentRowIndex, 1].Value = view.EntityType;
                            sheet.Cells[currentRowIndex, 2].Value = view.FullName;
                            sheet.Cells[currentRowIndex, 3].Value = view.ShortName;

                            grantsByRoleNameGroups = view.Grants.GroupBy(g => g.GrantedTo);
                            foreach (var grantsByRoleNameGroup in grantsByRoleNameGroups)
                            {
                                Grant firstGrant = grantsByRoleNameGroup.First();
                                int thisRoleColumnIndex = 0;
                                if (roleToHeaderMapping.ContainsKey(firstGrant.GrantedTo) == false)
                                {
                                    // Add another Role to the header
                                    thisRoleColumnIndex = roleColumnMaxIndex;
                                    roleToHeaderMapping.Add(firstGrant.GrantedTo, thisRoleColumnIndex);
                                    if (thisRoleColumnIndex <= 16384) sheet.Cells[headerRowIndex, thisRoleColumnIndex].Value = firstGrant.GrantedTo;
                                    roleColumnMaxIndex++;
                                }
                                else
                                {
                                    // Previously seen
                                    thisRoleColumnIndex = roleToHeaderMapping[firstGrant.GrantedTo];
                                }
                                if (thisRoleColumnIndex <= 16384) outputGrantstToCell(sheet.Cells[currentRowIndex, thisRoleColumnIndex], grantsByRoleNameGroup.ToList());
                            }

                            sheet.Row(currentRowIndex).OutlineLevel = 1;
                            currentRowIndex++;
                        }
                    }
                
                    range = sheet.Cells[headerRowIndex, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    try
                    {
                        table = sheet.Tables.Add(range, getExcelTableOrSheetSafeString(String.Format(SHEET_GRANTS_IN_DATABASE_FOR_ALL_ROLES, database.ShortName)));
                    }
                    catch (ArgumentException ex)
                    {
                        if (ex.Message == "Tablename is not unique")
                        {
                            table = sheet.Tables.Add(range, String.Format("{0}_1", getExcelTableOrSheetSafeString(String.Format(SHEET_GRANTS_IN_DATABASE_FOR_ALL_ROLES, database.ShortName))));
                        }
                    }
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Light18;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                
                    sheet.Column(2).Width = 30;
                    sheet.Column(3).Width = 20;

                    // Make the column for permissions headers angled downwards 45 degrees
                    for (int i = roleColumnBeginIndex; i <= table.Columns.Count; i++)
                    {
                        sheet.Cells[headerRowIndex, i].Style.TextRotation = 135;
                        sheet.Column(i).Width = 7;
                    }

                    // Format the cells
                    // But only if the number of columns isn't insanely high
                    if (sheet.Dimension.Columns <=10000)
                    {
                        ExcelRangeBase rangeToFormat = sheet.Cells[headerRowIndex + 1, 4, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        rangeToFormat.StyleName = "ShortPermissionStyle";

                        var cfR = sheet.ConditionalFormatting.AddEqual(rangeToFormat);
                        cfR.Style.Font.Color.Color = Color.Green;
                        cfR.Style.Fill.BackgroundColor.Color = Color.LightGreen;
                        cfR.Formula = "=\"R\"";

                        var cfCRUD = sheet.ConditionalFormatting.AddContainsText(rangeToFormat);
                        cfCRUD.Style.Font.Color.Color = Color.DarkRed;
                        cfCRUD.Style.Fill.BackgroundColor.Color = Color.Pink;
                        cfCRUD.Text = "C,R,U,D";

                        var cfCRU = sheet.ConditionalFormatting.AddContainsText(rangeToFormat);
                        cfCRU.Style.Font.Color.Color = Color.OrangeRed;
                        cfCRU.Style.Fill.BackgroundColor.Color = Color.Gold;
                        cfCRU.Text = "C,R,U";

                        var cfO = sheet.ConditionalFormatting.AddEqual(rangeToFormat);
                        cfO.Style.Font.Color.Color = Color.DarkBlue;
                        cfO.Style.Fill.BackgroundColor.Color = Color.Azure;
                        cfO.Formula = "=\"O\"";
                        
                        var cfOPlus = sheet.ConditionalFormatting.AddEqual(rangeToFormat);
                        cfOPlus.Style.Font.Color.Color = Color.DarkBlue;
                        cfOPlus.Style.Fill.BackgroundColor.Color = Color.LightBlue;
                        cfOPlus.Formula = "=\"O+\"";

                        rangeToFormat = sheet.Cells[headerRowIndex + 1, 3, sheet.Dimension.Rows, 3];

                        var cfFuture = sheet.ConditionalFormatting.AddContainsText(rangeToFormat);
                        cfFuture.Style.Fill.BackgroundColor.Color = Color.GreenYellow;
                        cfFuture.Text = "<";
                    }

                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                }
            }

            #endregion

            #region TOC sheet

            // TOC sheet again
            sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
            fillTableOfContentsSheet(sheet, excelReport);

            #endregion

            #region Save file

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
        }

        private void outputGrantstToCell(ExcelRangeBase cell, List<Grant> listOfGrants)
        {
            if (listOfGrants.Count == 0) return;
            
            string cellValue = String.Join(',', listOfGrants.OrderBy(g => g.PrivilegeOrder(privilegeOrderDict)).ToList().Select(g => g.PrivilegeDisplayShort(privilegeNamesShortDict)).ToArray());
            cell.Value = cellValue;
            if (cellValue.ToString().Length > 17)
            {
                cell.AddComment(String.Join('\n', listOfGrants.OrderBy(g => g.PrivilegeOrder(privilegeOrderDict)).ToList().Select(g => g.PrivilegeDisplayLong).ToArray()), "Snowflake");
                cell.Comment.AutoFit = true;
            }
        }
        
    }
}