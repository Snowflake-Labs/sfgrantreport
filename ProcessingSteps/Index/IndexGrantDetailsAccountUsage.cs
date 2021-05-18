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
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class IndexGrantDetailsAccountUsage : JobStepBase
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

            try
            {
                FileIOHelper.CreateFolder(this.FilePathMap.Data_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Data_Role_FolderPath());

                FileIOHelper.CreateFolder(this.FilePathMap.Report_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Grant_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Role_FolderPath());

                #region Grants ON and Grants TO grants for everything

                loggerConsole.Info("Process Grants ON and TO");

                List<RoleMember> grantsOfRolesList = new List<RoleMember>();

                List<Grant> grantsOnRolesList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Input_RoleShowGrantsToAndOn_FilePath(), new GrantGrantToRolesMap(), "Initiating login request with your identity provider");

                if (grantsOnRolesList != null)
                {
                    loggerConsole.Info("Loaded {0} ON and TO grants", grantsOnRolesList.Count);

                    // Unescape special names of objects
                    foreach (Grant grant in grantsOnRolesList)
                    {
                        grant.GrantedTo = grant.GrantedTo.Trim('"');
                        grant.GrantedBy = grant.GrantedBy.Trim('"');
                        // Apparently the ACCOUNT_USAGE casts 'NOTIFICATION_SUBSCRIPTION' to 'NOTIFICATION SUBSCRIPTION'
                        // And for others that have space
                        if (grant.ObjectType.Contains(' ') == true)
                        {
                            grant.ObjectType = grant.ObjectType.Replace(' ', '_');
                        }

                        // Escape periods
                        if (grant.EntityName.Contains('.') == true)
                        {
                            grant.EntityName = String.Format("\"{0}\"", grant.EntityName);
                        }
                        if (grant.DBName.Contains('.') == true)
                        {
                            grant.DBName = String.Format("\"{0}\"", grant.DBName);
                        }
                        if (grant.SchemaName.Contains('.') == true)
                        {
                            grant.SchemaName = String.Format("\"{0}\"", grant.SchemaName);
                        }
                        // Come up with ObjectName from combination of EntityName, etc.                        
                        if (grant.DBName.Length == 0)
                        {
                            // Account level object
                            grant.ObjectName = grant.EntityName;
                        }
                        else
                        {
                            if (grant.SchemaName.Length == 0)
                            {
                                // DATABASE
                                grant.ObjectName = grant.EntityName;
                                grant.DBName = grant.EntityName;
                            }
                            else
                            {
                                if (grant.ObjectType == "SCHEMA")
                                {
                                    grant.ObjectName = String.Format("{0}.{1}", grant.DBName, grant.EntityName);
                                }
                                else
                                {
                                    grant.ObjectName = String.Format("{0}.{1}.{2}", grant.DBName, grant.SchemaName, grant.EntityName);
                                }
                            }
                        }

                    }

                    grantsOnRolesList.RemoveAll(g => g.DeletedOn.HasValue == true);

                    grantsOnRolesList = grantsOnRolesList.OrderBy(g => g.ObjectType).ThenBy(g => g.ObjectName).ThenBy(g => g.GrantedTo).ToList();
                    FileIOHelper.WriteListToCSVFile<Grant>(grantsOnRolesList, new GrantMap(), FilePathMap.Report_RoleGrant_FilePath());

                    List<Grant> roleUsageGrantsList = grantsOnRolesList.Where(g => g.ObjectType == "ROLE" && g.Privilege == "USAGE").ToList();
                    if (roleUsageGrantsList != null)
                    {
                        foreach (Grant grant in roleUsageGrantsList)
                        {
                            RoleMember roleMember = new RoleMember();
                            roleMember.CreatedOn = grant.CreatedOn;
                            roleMember.Name = grant.ObjectName;
                            roleMember.GrantedBy = grant.GrantedBy;
                            roleMember.GrantedTo = grant.GrantedTo;
                            roleMember.ObjectType = grant.ObjectType;

                            grantsOfRolesList.Add(roleMember);
                        }

                        grantsOfRolesList = grantsOfRolesList.OrderBy(g => g.Name).ToList();
                    }
                    
                    #region Individual Object Types

                    loggerConsole.Info("Processing individual Object Types");

                    // Break them up by the type
                    var groupObjectTypesGrouped = grantsOnRolesList.GroupBy(g => g.ObjectType);
                    List<SingleStringRow> objectTypesList = new List<SingleStringRow>(groupObjectTypesGrouped.Count());
                    foreach (var group in groupObjectTypesGrouped)
                    {                        
                        loggerConsole.Info("Processing grants for {0}", group.Key);

                        SingleStringRow objectType = new SingleStringRow();
                        objectType.Value = group.Key;
                        objectTypesList.Add(objectType);

                        #region Save this set of grants for Object Type

                        List<Grant> grantsOfObjectTypeList = group.ToList();

                        // Save this set as is for one of the tables in report
                        FileIOHelper.WriteListToCSVFile<Grant>(grantsOfObjectTypeList, new GrantMap(), FilePathMap.Report_RoleGrant_ObjectType_FilePath(group.Key));

                        // Pivot each section into this kind of table
                        //             
                        // ObjectType | ObjectName | GrantedTo | OWNERSHIP | USAGE | REFERENCE | GrantN
                        // DATABASE   | SomeDB     | SomeRole  | X         | x+    |           |
                        // Where X+ means WithGrantOption=True
                        //       X  means WithGrantOption=False
                        List<ObjectTypeGrant> objectGrantsList = new List<ObjectTypeGrant>(grantsOfObjectTypeList.Count / 5);
                        Dictionary<string, int> privilegeToColumnDictionary = new Dictionary<string, int>(20);
                        
                        #endregion

                        #region Convert this set into pivot

                        List<string> listOfPrivileges = grantsOfObjectTypeList.Select(g => g.Privilege).Distinct().OrderBy(g => g).ToList();

                        // Make USAGE and OWNERSHIP be the first columns
                        switch (group.Key)
                        {
                            case "ACCOUNT":
                                break;

                            case "DATABASE":
                            case "FILE_FORMAT":
                            case "FUNCTION":
                            case "INTEGRATION":
                            case "PROCEDURE":
                            case "ROLE":
                            case "SCHEMA":
                            case "SEQUENCE":
                            case "WAREHOUSE":
                                listOfPrivileges.Remove("OWNERSHIP");
                                listOfPrivileges.Insert(0, "OWNERSHIP");
                                listOfPrivileges.Remove("USAGE");
                                listOfPrivileges.Insert(1, "USAGE");
                                break;

                            case "EXTERNAL_TABLE":
                            case "MANAGED_ACCOUNT":
                            case "MASKING_POLICY":
                            case "MATERIALIZED_VIEW":
                            case "NETWORK_POLICY":
                            case "NOTIFICATION_SUBSCRIPTION":
                            case "PIPE":
                            case "RESOURCE_MONITOR":
                            case "SHARE":
                            case "STAGE":
                            case "STREAM":
                            case "TABLE":
                            case "TASK":
                            case "USER":
                            case "VIEW":
                                listOfPrivileges.Remove("OWNERSHIP");
                                listOfPrivileges.Insert(0, "OWNERSHIP");
                                break;

                            default:
                                break;
                        }
                        for (int i = 0; i < listOfPrivileges.Count; i++)
                        {
                            privilegeToColumnDictionary.Add(listOfPrivileges[i], i);
                        }

                        ObjectTypeGrant latestGrantRow = new ObjectTypeGrant();
                        foreach (Grant grant in grantsOfObjectTypeList)
                        {
                            // Loop through rows, starting new objects for each combination of ObjectType+ObjectName+GrantedTo when necessary
                            // ObjectType is always the same in this grouping 
                            // ObjectName
                            if (latestGrantRow.ObjectType != grant.ObjectType ||
                                latestGrantRow.ObjectName != grant.ObjectName ||
                                latestGrantRow.GrantedTo  != grant.GrantedTo)
                            {
                                // Need to start new row
                                latestGrantRow = new ObjectTypeGrant();
                                latestGrantRow.ObjectType = grant.ObjectType;
                                latestGrantRow.ObjectName = grant.ObjectName;
                                latestGrantRow.DBName = grant.DBName;
                                latestGrantRow.SchemaName = grant.SchemaName;
                                latestGrantRow.EntityName = grant.EntityName;
                                latestGrantRow.GrantedTo = grant.GrantedTo;

                                objectGrantsList.Add(latestGrantRow);
                            }

                            // Find out which column to use
                            int privilegeColumnNumber = privilegeToColumnDictionary[grant.Privilege];

                            switch (privilegeColumnNumber)
                            {
                                case 0:
                                    latestGrantRow.Privilege0 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 1:
                                    latestGrantRow.Privilege1 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 2:
                                    latestGrantRow.Privilege2 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 3:
                                    latestGrantRow.Privilege3 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 4:
                                    latestGrantRow.Privilege4 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 5:
                                    latestGrantRow.Privilege5 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 6:
                                    latestGrantRow.Privilege6 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 7:
                                    latestGrantRow.Privilege7 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 8:
                                    latestGrantRow.Privilege8 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 9:
                                    latestGrantRow.Privilege9 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 10:
                                    latestGrantRow.Privilege10 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 11:
                                    latestGrantRow.Privilege11 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 12:
                                    latestGrantRow.Privilege12 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 13:
                                    latestGrantRow.Privilege13 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 14:
                                    latestGrantRow.Privilege14 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 15:
                                    latestGrantRow.Privilege15 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 16:
                                    latestGrantRow.Privilege16 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 17:
                                    latestGrantRow.Privilege17 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 18:
                                    latestGrantRow.Privilege18 = grant.DisplaySettingWithGrantOption;
                                    break;
                                case 19:
                                    latestGrantRow.Privilege19 = grant.DisplaySettingWithGrantOption;
                                    break;
                                default:
                                    // Can't fit more than 20 privileges
                                    logger.Warn("More then 20 Privileges reached with {0} privilege for object type {1}", grant.Privilege, grant.ObjectType);
                                    break;
                            }
                        }

                        List<string> privilegeColumnNames = new List<string>(privilegeToColumnDictionary.Count);
                        for (int i = 0; i < privilegeToColumnDictionary.Count; i++)
                        {
                            privilegeColumnNames.Add(String.Empty);
                        }
                        foreach (var entry in privilegeToColumnDictionary)
                        {
                            privilegeColumnNames[entry.Value] = entry.Key;
                        }

                        // Save the pivot
                        FileIOHelper.WriteListToCSVFile<ObjectTypeGrant>(objectGrantsList, new ObjectTypeGrantMap(privilegeColumnNames), FilePathMap.Report_RoleGrant_ObjectType_Pivoted_FilePath(group.Key));

                        #endregion
                    }

                    FileIOHelper.WriteListToCSVFile<SingleStringRow>(objectTypesList, new SingleStringRowMap(), FilePathMap.Report_RoleGrant_ObjectTypes_FilePath());

                    #endregion
                }

                #endregion


                #region Grants OF - Members of Roles (Roles and Users)

                loggerConsole.Info("Process Grants OF Users");

                List<RoleMember> grantsOfUsersList = FileIOHelper.ReadListFromCSVFile<RoleMember>(FilePathMap.Input_RoleShowGrantsOf_FilePath(), new RoleMemberGrantsToUsersMap(), "Initiating login request with your identity provider");
                if (grantsOfUsersList != null)
                {
                    foreach (RoleMember roleMember in grantsOfUsersList)
                    {
                        // Unescape special names of roles
                        roleMember.Name = roleMember.Name.Trim('"');
                        roleMember.GrantedTo = roleMember.GrantedTo.Trim('"');
                        roleMember.GrantedBy = roleMember.GrantedBy.Trim('"');
                    }

                    // Remove deleted items 
                    grantsOfUsersList.RemoveAll(g => g.DeletedOn.HasValue == true);

                    grantsOfUsersList = grantsOfUsersList.OrderBy(g => g.Name).ToList();

                    List<RoleMember> grantsOfRolesAndUsersList = new List<RoleMember>();
                    grantsOfRolesAndUsersList.AddRange(grantsOfRolesList);
                    grantsOfRolesAndUsersList.AddRange(grantsOfUsersList);
                    
                    FileIOHelper.WriteListToCSVFile<RoleMember>(grantsOfRolesAndUsersList, new RoleMemberMap(), FilePathMap.Report_RoleMember_FilePath());
                }

                #endregion

                // Come up with roles list for later steps too
                if (grantsOnRolesList == null) grantsOnRolesList = new List<Grant>();
                
                List<Role> rolesList = new List<Role>();
                List<string> rolesInThisAccountList = grantsOnRolesList.Where(g => g.ObjectType == "ROLE").Select(g => g.ObjectName).Distinct().ToList();
                foreach (string roleName in rolesInThisAccountList)
                {
                    Role role = new Role();
                    role.CreatedOn = DateTime.Now;
                    role.Name = roleName;
                    
                    rolesList.Add(role);
                }
                
                if (rolesList.Where(r => r.Name == "ACCOUNTADMIN").Count() == 0)
                {
                    Role role = new Role();
                    role.CreatedOn = DateTime.Now;
                    role.Name = "ACCOUNTADMIN";

                    rolesList.Add(role);
                }

                if (rolesList.Where(r => r.Name == "PUBLIC").Count() == 0)
                {
                    Role role = new Role();
                    role.CreatedOn = DateTime.Now;
                    role.Name = "PUBLIC";

                    rolesList.Add(role);
                }

                FileIOHelper.WriteListToCSVFile<Role>(rolesList, new RoleShowRolesMap(), FilePathMap.Data_ShowRoles_FilePath());

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
            if (programOptions.InputFolderPath != null && programOptions.InputFolderPath.Length > 0)
            {
                logger.Trace("Input Folder Path is not empty. Will execute");
                loggerConsole.Trace("Input Folder Path is not empty. Will execute");
                return true;
            }
            else
            {
                logger.Trace("Input Folder Path is not empty. Skipping this step");
                loggerConsole.Trace("Input Folder Path is not empty. Skipping this step");
                return false;
            }
        }
    }
}