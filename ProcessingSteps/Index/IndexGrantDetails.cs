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
    public class IndexGrantDetails : JobStepBase
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
                FileIOHelper.CreateFolder(this.FilePathMap.Report_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Grant_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Role_FolderPath());

                #region Grants OF - Members of Roles (Roles and Users)

                loggerConsole.Info("Process Grants OF");

                List<RoleMember> grantsOfRolesAndUsersList = FileIOHelper.ReadListFromCSVFile<RoleMember>(FilePathMap.Data_RoleShowGrantsOf_FilePath(), new RoleMemberShowGrantsMap(), new string[] {"No data returned", "SQL compilation error", "does not exist"});
                if (grantsOfRolesAndUsersList != null)
                {
                    foreach (RoleMember roleMember in grantsOfRolesAndUsersList)
                    {
                        // Unescape special names of roles
                        roleMember.Name = roleMember.Name.Trim('"');
                        roleMember.GrantedTo = roleMember.GrantedTo.Trim('"');
                        roleMember.GrantedBy = roleMember.GrantedBy.Trim('"');
                    }

                    grantsOfRolesAndUsersList = grantsOfRolesAndUsersList.OrderBy(g => g.ObjectType).ThenBy(g => g.Name).ToList();
                    
                    FileIOHelper.WriteListToCSVFile<RoleMember>(grantsOfRolesAndUsersList, new RoleMemberMap(), FilePathMap.Report_RoleMember_FilePath());
                }

                #endregion

                #region Grants ON and Grants TO grants for everything

                loggerConsole.Info("Process Grants ON, TO and FUTURE grants");

                List<Grant> grantsNonUniqueList = new List<Grant>();

                List<Grant> grantsOnRolesList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Data_RoleShowGrantsOn_FilePath(), new GrantShowGrantsMap(), new string[] {"No data returned", "SQL compilation error", "does not exist"});
                if (grantsOnRolesList != null)
                {
                    loggerConsole.Info("Granted ON {0} grants", grantsOnRolesList.Count);
                    grantsNonUniqueList.AddRange(grantsOnRolesList);
                }

                List<Grant> grantsToRolesList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Data_RoleShowGrantsTo_FilePath(), new GrantShowGrantsMap(), new string[] {"No data returned", "SQL compilation error", "does not exist"});
                if (grantsToRolesList != null)
                {
                    loggerConsole.Info("Granted TO {0} grants", grantsToRolesList.Count);
                    grantsNonUniqueList.AddRange(grantsToRolesList);
                }

                List<Grant> grantsFutureDatabasesList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Data_FutureGrantsInDatabases_FilePath(), new GrantShowFutureGrantsMap(), new string[] {"No data returned", "SQL compilation error", "does not exist"});
                if (grantsFutureDatabasesList != null)
                {
                    loggerConsole.Info("Future Grants on Databases {0} grants", grantsFutureDatabasesList.Count);
                    grantsNonUniqueList.AddRange(grantsFutureDatabasesList);
                }

                List<Grant> grantsFutureSchemasList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Data_FutureGrantsInSchemas_FilePath(), new GrantShowFutureGrantsMap(), new string[] {"No data returned", "SQL compilation error", "does not exist"});
                if (grantsFutureSchemasList != null)
                {
                    loggerConsole.Info("Future Grants on Schemas {0} grants", grantsFutureSchemasList.Count);
                    grantsNonUniqueList.AddRange(grantsFutureSchemasList);
                }

                loggerConsole.Info("All Grants on Schemas {0} grants", grantsNonUniqueList.Count);

                #region Remove duplicates

                loggerConsole.Info("Removing duplicate grants");

                // Now remove duplicate USAGE and OWNERSHIP rows using these kinds of IDs
                // OWNERSHIP-ROLE-AAD_PROVISIONER-USERADMIN
                // USAGE-ROLE-AAD_PROVISIONER-USERADMIN
                // These occur only on ROLEs and because a role in hierarchy can be seen when parent says SHOW GRANTS ON and child says SHOW GRANTS TO
                List<Grant> grantsUniqueList = new List<Grant>(grantsNonUniqueList.Count);
                var uniqueGrantsGrouped = grantsNonUniqueList.GroupBy(g => g.UniqueIdentifier);
                foreach (var group in uniqueGrantsGrouped)
                {
                    grantsUniqueList.Add(group.First());
                }

                // Unescape special names of objects
                foreach (Grant grant in grantsUniqueList)
                {
                    grant.GrantedTo = grant.GrantedTo.Trim('"');
                    if (grant.GrantedBy != null) grant.GrantedBy = grant.GrantedBy.Trim('"');
                }

                grantsUniqueList = grantsUniqueList.OrderBy(g => g.ObjectType).ThenBy(g => g.ObjectName).ThenBy(g => g.GrantedTo).ToList();
                FileIOHelper.WriteListToCSVFile<Grant>(grantsUniqueList, new GrantMap(), FilePathMap.Report_RoleGrant_FilePath());

                #endregion
                
                #region Individual Object Types

                loggerConsole.Info("Processing individual Object Types");

                // Break them up by the type
                var groupObjectTypesGrouped = grantsUniqueList.GroupBy(g => g.ObjectType);
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