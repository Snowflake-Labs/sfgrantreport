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

using System;
using System.IO;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class FilePathMap
    {

        private const string DATA_FOLDER_NAME = "DATA";
        private const string CONNECTION_CONTEXT_FOLDER_NAME = "CONN";

        #region Constants for Step Timing report

        private const string TIMING_REPORT_FILE_NAME = "StepDurations.csv";

        #endregion

        #region Constructor and properties

        public ProgramOptions ProgramOptions { get; set; }

        public FilePathMap(ProgramOptions programOptions)
        {
            this.ProgramOptions = programOptions;
        }

        #endregion

        #region Step Timing Report

        public string StepTimingReportFilePath()
        {
            return Path.Combine(this.ProgramOptions.ReportFolderPath, TIMING_REPORT_FILE_NAME);
        }

        #endregion

        #region SQL Files packaged with product and produced on the fly

        public string Data_CurrentContext_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "CurrentContext.sql");
        }

        public string Data_UsersAndRoles_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ListOfUsersAndRoles.sql");
        }

        public string Data_RoleGrantsOn_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ShowGrantsOnRole.sql");
        }

        public string Data_RoleGrantsTo_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ShowGrantsToRole.sql");
        }

        public string Data_RoleGrantsOf_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ShowGrantsOfRole.sql");
        }

        public string Data_FutureGrantsInDatabases_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ShowFutureGrantsInDatabases.sql");
        }

        public string Data_FutureGrantsInSchemas_SQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "ShowFutureGrantsInSchemas.sql");
        }

        public string DescribeUserSQLQuery_FilePath()
        {
            return Path.Combine(this.Data_FolderPath(), "DescribeUser.sql");
        }

        #endregion

        #region Data Retrieval Folders

        public string Data_FolderPath()
        {
            return Path.Combine(this.ProgramOptions.ReportFolderPath, "DATA");
        }

        public string Data_Connection_FolderPath()
        {
            return Path.Combine(this.Data_FolderPath(), "CONN");
        }

        public string Data_Account_FolderPath()
        {
            return Path.Combine(this.Data_FolderPath(), "ACCT");
        }

        public string Data_User_FolderPath()
        {
            return Path.Combine(this.Data_FolderPath(), "USER");
        }

        public string Data_Role_FolderPath()
        {
            return Path.Combine(this.Data_FolderPath(), "ROLE");
        }

        public string Data_Grant_FolderPath()
        {
            return Path.Combine(this.Data_FolderPath(), "GRANT");
        }

        #endregion

        #region Report Folders

        public string Report_FolderPath()
        {
            return Path.Combine(this.ProgramOptions.ReportFolderPath, "RPT");
        }

        public string Report_Connection_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "CONN");
        }

        public string Report_Account_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "ACCT");
        }

        public string Report_User_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "USER");
        }

        public string Report_Role_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "ROLE");
        }

        public string Report_Grant_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "GRANT");
        }

        public string Report_GraphViz_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "GRAPHVIZ");
        }

        public string Report_Diagram_SVG_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "DIAGRAM", "SVG");
        }

        public string Report_Diagram_PNG_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "DIAGRAM", "PNG");
        }

        public string Report_Diagram_PDF_FolderPath()
        {
            return Path.Combine(this.Report_FolderPath(), "DIAGRAM", "PDF");
        }

        #endregion

        #region Users

        public string Data_ShowUsers_FilePath()
        {
            return Path.Combine(this.Data_User_FolderPath(), "SHOW_USERS.csv");
        }
        
        public string Data_DescribeUser_FilePath(string userName)
        {
            return Path.Combine(this.Data_User_FolderPath(), String.Format("DESCRIBE_USER.{0}.csv", getFileSystemSafeString(userName)));
        }

        public string Report_UserDetail_FilePath()
        {
            return Path.Combine(this.Report_User_FolderPath(), "USERS.csv");
        }

        #endregion

        #region Context

        public string Data_CurrentAccount_FilePath()
        {
            return Path.Combine(this.Data_Account_FolderPath(), "CURRENT_ACCOUNT.csv");
        }

        public string Data_CurrentRegion_FilePath()
        {
            return Path.Combine(this.Data_Account_FolderPath(), "CURRENT_REGION.csv");
        }

        public string Data_CurrentVersion_FilePath()
        {
            return Path.Combine(this.Data_Account_FolderPath(), "CURRENT_VERSION.csv");
        }

        public string Data_CurrentClient_FilePath()
        {
            return Path.Combine(this.Data_Account_FolderPath(), "CURRENT_CLIENT.csv");
        }

        public string Data_CurrentUser_FilePath()
        {
            return Path.Combine(this.Data_Connection_FolderPath(), "CURRENT_USER.csv");
        }

        public string Data_CurrentRole_FilePath()
        {
            return Path.Combine(this.Data_Connection_FolderPath(), "CURRENT_ROLE.csv");
        }

        public string Data_CurrentWarehouse_FilePath()
        {
            return Path.Combine(this.Data_Connection_FolderPath(), "CURRENT_WAREHOUSE.csv");
        }

        public string Data_CurrentDatabase_FilePath()
        {
            return Path.Combine(this.Data_Connection_FolderPath(), "CURRENT_DATABASE.csv");
        }

        public string Data_CurrentSchema_FilePath()
        {
            return Path.Combine(this.Data_Connection_FolderPath(), "CURRENT_SCHEMA.csv");
        }

        #endregion

        #region Roles and Grants

        public string Data_ShowRoles_FilePath()
        {
            return Path.Combine(this.Data_Role_FolderPath(), "SHOW_ROLES.csv");
        }

        public string Data_RoleShowGrantsOn_FilePath()
        {
            return Path.Combine(this.Data_Grant_FolderPath(), "ROLE_GRANTS_ON.csv");
        }

        public string Data_RoleShowGrantsTo_FilePath()
        {
            return Path.Combine(this.Data_Grant_FolderPath(), "ROLE_GRANTS_TO.csv");
        }

        public string Data_RoleShowGrantsOf_FilePath()
        {
            return Path.Combine(this.Data_Grant_FolderPath(), "ROLE_GRANTS_OF.csv");
        }

        public string Data_FutureGrantsInDatabases_FilePath()
        {
            return Path.Combine(this.Data_Grant_FolderPath(), "FUTURE_GRANTS_DATABASES.csv");
        }

        public string Data_FutureGrantsInSchemas_FilePath()
        {
            return Path.Combine(this.Data_Grant_FolderPath(), "FUTURE_GRANTS_SCHEMAS.csv");
        }

        public string Input_RoleShowGrantsOf_FilePath()
        {
            return Path.Combine(this.ProgramOptions.InputFolderPath, "GRANTS_TO_USERS.csv");
        }

        public string Input_RoleShowGrantsToAndOn_FilePath()
        {
            return Path.Combine(this.ProgramOptions.InputFolderPath, "GRANTS_TO_ROLES.csv");
        }

        public string Report_RoleDetail_FilePath()
        {
            return Path.Combine(this.Report_Role_FolderPath(), "ROLES.csv");
        }

        public string Report_RoleDetail_RoleAndItsRelations_FilePath(string roleName)
        {
            return Path.Combine(this.Report_Role_FolderPath(), String.Format("ROLES.Related.{0}.csv", getFileSystemSafeString(roleName)));
        }

        public string Report_RoleOwnerSankey_FilePath()
        {
            return Path.Combine(this.Report_Role_FolderPath(), "ROLES_OWNER_SANKEY.csv");
        }

        public string Report_RoleHierarchy_FilePath()
        {
            return Path.Combine(this.Report_Role_FolderPath(), "ROLES_HIERARCHY.csv");
        }

        public string Report_RoleHierarchy_RoleAndItsRelations_FilePath(string roleName)
        {
            return Path.Combine(this.Report_Role_FolderPath(), String.Format("ROLES_HIERARCHY.Related.{0}.csv", getFileSystemSafeString(roleName)));
        }

        public string Report_RoleGrant_FilePath()
        {
            return Path.Combine(this.Report_Grant_FolderPath(), "GRANTS.csv");
        }

        public string Report_RoleGrant_ObjectType_FilePath(string objectType)
        {
            return Path.Combine(this.Report_Grant_FolderPath(), String.Format("GRANTS.{0}.csv", objectType));
        }

        public string Report_RoleGrant_ObjectType_Pivoted_FilePath(string objectType)
        {
            return Path.Combine(this.Report_Grant_FolderPath(), String.Format("GRANTSPIVOT.{0}.csv", objectType));
        }

        public string Report_RoleGrant_ObjectTypes_FilePath()
        {
            return Path.Combine(this.Report_Grant_FolderPath(), "GRANTS.ObjectType.csv");
        }

        public string Report_RoleMember_FilePath()
        {
            return Path.Combine(this.Report_Role_FolderPath(), "ROLE_MEMBERS.csv");
        }

        public string Report_RoleGrant_Differences_FilePath()
        {
            return Path.Combine(this.Report_Grant_FolderPath(), "GRANTDIFFERENCES.csv");
        }

        public string UsersRolesAndGrantsExcelReportFilePath()
        {
            if (this.ProgramOptions.ReportJob.Connection != null && this.ProgramOptions.ReportJob.Connection.Length > 0)
            {
                return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.ALL.{0}.{1:yyyyMMddHHmm}.xlsx", this.ProgramOptions.ReportJob.Connection, this.ProgramOptions.ReportJob.DataRetrievedOnUtc));
            }
            else
            {
                return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.ALL.{0}.{1:yyyyMMddHHmm}.xlsx", new DirectoryInfo(this.ProgramOptions.InputFolderPath).Name, this.ProgramOptions.ReportJob.DataRetrievedOnUtc));
            }
        }

        public string UsersRolesAndGrantsExcelReportFilePath(string sheetSetName)
        {
            if (this.ProgramOptions.ReportJob.Connection != null && this.ProgramOptions.ReportJob.Connection.Length > 0)
            {
                return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.{0}.{1}.{2:yyyyMMddHHmm}.xlsx", sheetSetName, this.ProgramOptions.ReportJob.Connection, this.ProgramOptions.ReportJob.DataRetrievedOnUtc));
            }
            else
            {
                return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.{0}.{1}.{2:yyyyMMddHHmm}.xlsx", sheetSetName, new DirectoryInfo(this.ProgramOptions.InputFolderPath).Name, this.ProgramOptions.ReportJob.DataRetrievedOnUtc));
            }
        }

        public string GrantsDifferencesExcelReportFilePath()
        {
            return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.Differences.{0:yyyyMMddHHmm}.xlsx", DateTime.UtcNow));
        }

        #endregion

        #region Graph Visializations

        public string Report_GraphViz_RoleAndItsRelationsGrants_FilePath(string roleName)
        {
            return Path.Combine(this.Report_GraphViz_FolderPath(), String.Format("{0}.gv", getFileSystemSafeString(roleName)));
        }

        public string Report_Diagram_SVG_RoleAndItsRelationsGrants_FilePath(string roleName, bool returnAbsolutePath)
        {
            string reportFilePath = Path.Combine(this.Report_Diagram_SVG_FolderPath(), String.Format("{0}.svg", getFileSystemSafeString(roleName)));

            if (returnAbsolutePath == false)
            {
                reportFilePath = Path.GetRelativePath(this.ProgramOptions.ReportFolderPath, reportFilePath);
            }
            return reportFilePath;
        }

        public string Report_Diagram_PNG_RoleAndItsRelationsGrants_FilePath(string roleName, bool returnAbsolutePath)
        {
            string reportFilePath = Path.Combine(this.Report_Diagram_PNG_FolderPath(), String.Format("{0}.png", getFileSystemSafeString(roleName)));

            if (returnAbsolutePath == false)
            {
                reportFilePath = Path.GetRelativePath(this.ProgramOptions.ReportFolderPath, reportFilePath);
            }
            return reportFilePath;
        }

        public string Report_Diagram_PDF_RoleAndItsRelationsGrants_FilePath(string roleName, bool returnAbsolutePath)
        {
            string reportFilePath = Path.Combine(this.Report_Diagram_PDF_FolderPath(), String.Format("{0}.pdf", getFileSystemSafeString(roleName)));

            if (returnAbsolutePath == false)
            {
                reportFilePath = Path.GetRelativePath(this.ProgramOptions.ReportFolderPath, reportFilePath);
            }
            return reportFilePath;
        }

        public string UsersRolesAndGrantsWebReportFilePath()
        {
            return Path.Combine(this.ProgramOptions.ReportFolderPath, String.Format("SFGrantReport.{0}.{1:yyyyMMddHHmm}.html", this.ProgramOptions.ReportJob.Connection, this.ProgramOptions.ReportJob.DataRetrievedOnUtc));
        }

        #endregion

        #region Helper function for various entity naming

        public static string getFileSystemSafeString(string fileOrFolderNameToClear)
        {
            if (fileOrFolderNameToClear == null) fileOrFolderNameToClear = String.Empty;

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileOrFolderNameToClear = fileOrFolderNameToClear.Replace(c, '-');
            }

            char[] otherInvalidChars = { '"' };
            foreach (var c in otherInvalidChars)
            {
                fileOrFolderNameToClear = fileOrFolderNameToClear.Replace(c, '-');
            }   

            return fileOrFolderNameToClear;
        }

        #endregion

    }
}