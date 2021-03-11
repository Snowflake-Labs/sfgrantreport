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

using Snowflake.GrantReport.ReportObjects;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class ExtractUsersRolesGrants : JobStepBase
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
                FileIOHelper.CreateFolder(this.FilePathMap.Data_User_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Data_Role_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Data_Grant_FolderPath());

                #region List of roles and users

                loggerConsole.Info("Retrieving list of roles and users");

                StringBuilder sb = new StringBuilder(1024);
                
                sb.AppendFormat("ALTER SESSION SET QUERY_TAG='Snowflake Grant Report Version {0}';", Assembly.GetEntryAssembly().GetName().Version); sb.AppendLine();

                sb.AppendLine("!set output_format=csv");
                sb.AppendLine("!set header=true");

                sb.AppendLine("USE ROLE SECURITYADMIN;");
                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_ShowRoles_FilePath()); sb.AppendLine();
                sb.AppendLine("SHOW ROLES;");
                sb.AppendLine(@"!spool off");

                sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_ShowUsers_FilePath()); sb.AppendLine();
                sb.AppendLine("SHOW USERS;");
                sb.AppendLine(@"!spool off");

                FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.Data_UsersAndRoles_SQLQuery_FilePath(), false);

                snowSQLDriver.ExecuteSQLStatementsInFile(this.FilePathMap.Data_UsersAndRoles_SQLQuery_FilePath(), programOptions.ReportFolderPath);

                #endregion

                #region User details

                List<User> usersList = FileIOHelper.ReadListFromCSVFile<User>(FilePathMap.Data_ShowUsers_FilePath(), new UserShowUsersMinimalMap());
                if (usersList != null)
                {
                    loggerConsole.Info("Retrieving user details for {0} users", usersList.Count);

                    sb = new StringBuilder(256 * usersList.Count);
                
                    sb.AppendFormat("ALTER SESSION SET QUERY_TAG='Snowflake Grant Report Version {0}';", Assembly.GetEntryAssembly().GetName().Version); sb.AppendLine();

                    sb.AppendLine("!set output_format=csv");
                    sb.AppendLine("!set header=true");

                    sb.AppendLine("USE ROLE SECURITYADMIN;");
                    sb.AppendLine("USE ROLE ACCOUNTADMIN;");

                    for (int i = 0; i< usersList.Count; i++)
                    {
                        User user = usersList[i];

                        sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_DescribeUser_FilePath(user.NAME)); sb.AppendLine();
                        sb.AppendFormat("DESCRIBE USER {0};", quoteObjectIdentifier(user.NAME)); sb.AppendLine();
                        sb.AppendLine(@"!spool off");
                    }

                    FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.DescribeUserSQLQuery_FilePath(), false);

                    snowSQLDriver.ExecuteSQLStatementsInFile(FilePathMap.DescribeUserSQLQuery_FilePath(), programOptions.ReportFolderPath);
                }

                #endregion

                #region Role Grants

                List<Role> rolesList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Data_ShowRoles_FilePath(), new RoleShowRolesMinimalMap());
                if (rolesList != null)
                {
                    #region Role Grants On

                    loggerConsole.Info("Retrieving role grants ON for {0} roles", rolesList.Count);

                    sb = new StringBuilder(256 * rolesList.Count);

                    sb.AppendFormat("ALTER SESSION SET QUERY_TAG='Snowflake Grant Report Version {0}';", Assembly.GetEntryAssembly().GetName().Version); sb.AppendLine();

                    sb.AppendLine("!set output_format=csv");
                    sb.AppendLine("!set header=true");

                    sb.AppendLine("USE ROLE SECURITYADMIN;");
                    sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_RoleShowGrantsOn_FilePath()); sb.AppendLine();
                    for (int i = 0; i< rolesList.Count; i++)
                    {
                        Role role = rolesList[i];
                        sb.AppendFormat("SHOW GRANTS ON ROLE {0};", quoteObjectIdentifier(role.Name)); sb.AppendLine();
                        if (i == 0) { sb.AppendLine("!set header=false"); }
                    }
                    sb.AppendLine(@"!spool off");

                    FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.Data_RoleGrantsOn_SQLQuery_FilePath(), false);

                    snowSQLDriver.ExecuteSQLStatementsInFile(FilePathMap.Data_RoleGrantsOn_SQLQuery_FilePath(), programOptions.ReportFolderPath);

                    #endregion

                    #region Role Grants To

                    loggerConsole.Info("Retrieving role grants TO for {0} roles", rolesList.Count);

                    sb = new StringBuilder(256 * rolesList.Count);

                    sb.AppendFormat("ALTER SESSION SET QUERY_TAG='Snowflake Grant Report Version {0}';", Assembly.GetEntryAssembly().GetName().Version); sb.AppendLine();

                    sb.AppendLine("!set output_format=csv");
                    sb.AppendLine("!set header=true");

                    sb.AppendLine("USE ROLE SECURITYADMIN;");
                    sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_RoleShowGrantsTo_FilePath()); sb.AppendLine();
                    for (int i = 0; i< rolesList.Count; i++)
                    {
                        Role role = rolesList[i];
                        sb.AppendFormat("SHOW GRANTS TO ROLE {0};", quoteObjectIdentifier(role.Name)); sb.AppendLine();
                        if (i == 0) { sb.AppendLine("!set header=false"); }
                    }
                    sb.AppendLine(@"!spool off");

                    FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.Data_RoleGrantsTo_SQLQuery_FilePath(), false);

                    snowSQLDriver.ExecuteSQLStatementsInFile(FilePathMap.Data_RoleGrantsTo_SQLQuery_FilePath(), programOptions.ReportFolderPath);

                    #endregion

                    #region Role Grants Of

                    loggerConsole.Info("Retrieving role grants OF for {0} roles", rolesList.Count);

                    sb = new StringBuilder(256 * rolesList.Count);
                    sb.AppendFormat("ALTER SESSION SET QUERY_TAG='Snowflake Grant Report Version {0}';", Assembly.GetEntryAssembly().GetName().Version); sb.AppendLine();

                    sb.AppendLine("!set output_format=csv");
                    sb.AppendLine("!set header=true");

                    sb.AppendLine("USE ROLE SECURITYADMIN;");
                    sb.AppendFormat("!spool \"{0}\"", FilePathMap.Data_RoleShowGrantsOf_FilePath()); sb.AppendLine();
                    for (int i = 0; i< rolesList.Count; i++)
                    {
                        Role role = rolesList[i];
                        // Output header for only the first item
                        sb.AppendFormat("SHOW GRANTS OF ROLE {0};", quoteObjectIdentifier(role.Name)); sb.AppendLine();
                        if (i == 0) { sb.AppendLine("!set header=false"); }
                    }
                    sb.AppendLine(@"!spool off");

                    FileIOHelper.SaveFileToPath(sb.ToString(), FilePathMap.Data_RoleGrantsOf_SQLQuery_FilePath(), false);

                    snowSQLDriver.ExecuteSQLStatementsInFile(FilePathMap.Data_RoleGrantsOf_SQLQuery_FilePath(), programOptions.ReportFolderPath);

                    #endregion
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