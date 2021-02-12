using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class IndexRoleAndGrantHierarchy : JobStepBase
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
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Role_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Grant_FolderPath());

                List<Role> rolesList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Data_ShowRoles_FilePath(), new RoleShowRolesMap());
                List<RoleMember> grantsOfRolesList = FileIOHelper.ReadListFromCSVFile<RoleMember>(FilePathMap.Report_RoleMember_FilePath(), new RoleMemberMap());
                if (rolesList != null)
                {
                    Dictionary<string, Role> rolesDict = rolesList.ToDictionary(k => k.Name, r => r);

                    loggerConsole.Info("Parsing role details for {0} roles", rolesList.Count);

                    int j = 0;

                    foreach(Role role in rolesList)
                    {
                        logger.Trace("Parsing details for role {0}", role.Name);

                        if (String.Compare(role.Name, quoteObjectIdentifier(role.Name), true, CultureInfo.InvariantCulture) == 0)
                        {
                            role.IsObjectIdentifierSpecialCharacters = false;
                        }
                        else
                        {
                            role.IsObjectIdentifierSpecialCharacters = true;
                        }

                        // Parse the list of users and child roles
                        if (grantsOfRolesList != null)
                        {
                            List<RoleMember> grantsOfRoleToUserList = grantsOfRolesList.Where(g => g.Name == role.Name && g.ObjectType == "USER").ToList();
                            if (grantsOfRoleToUserList != null && grantsOfRoleToUserList.Count > 0)
                            {
                                role.AssignedUsers = String.Join(',', grantsOfRoleToUserList.Select(g => g.GrantedTo).ToArray());
                            }
                        }

                        j++;
                        if (j % 10 == 0)
                        {
                            Console.Write("{0}.", j);
                        }
                    }
                    Console.WriteLine("Done {0} items", rolesList.Count);

                    loggerConsole.Info("Parsing role usage hierarchy");

                    // Now load the all the Roles and their USAGE permission to build parent and child hierarchy
                    List<Grant> grantsToRolesList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("ROLE"), new GrantMap());
                    if (grantsToRolesList != null)
                    {
                        List<Grant> grantsToRolesUsageList = grantsToRolesList.Where(g => g.Privilege == "USAGE").ToList();
                        if (grantsToRolesUsageList != null)
                        {
                            foreach (Grant grant in grantsToRolesUsageList)
                            {
                                Role roleBeingGranted;
                                Role grantedToRole;

                                if (rolesDict.TryGetValue(grant.ObjectName, out roleBeingGranted) == true &&
                                    rolesDict.TryGetValue(grant.GrantedTo, out grantedToRole) == true)
                                {
                                    grantedToRole.ChildRoles.Add(roleBeingGranted);
                                    roleBeingGranted.ParentRoles.Add(grantedToRole);
                                }
                            }
                        }
                    }

                    loggerConsole.Info("Analyzing role types");

                    // Load selected types of grants to use to determine whether the role is Functional or Access
                    // Going to just use SCHEMA and TABLE
                    List<Grant> grantsToSchemaList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("SCHEMA"), new GrantMap());
                    List<Grant> grantsToTableList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("TABLE"), new GrantMap());
                    List<Grant> grantsToViewList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("VIEW"), new GrantMap());
                    List<Grant> grantsToRoleList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("ROLE"), new GrantMap());

                    // Detect role types and inheritance rollups
                    foreach(Role role in rolesList)
                    {
                        // Detect built-in roles and 
                        if (role.Name == "ACCOUNTADMIN" || role.Name == "SECURITYADMIN" || role.Name == "USERADMIN" || role.Name == "SYSADMIN" || role.Name == "PUBLIC")
                        {
                            role.Type = RoleType.BuiltIn;
                        }
                        else if (role.Name == "AAD_PROVISIONER" || role.Name == "OKTA_PROVISIONER" || role.Name == "GENERIC_SCIM_PROVISIONER")
                        {
                            role.Type = RoleType.SCIM;
                        }
                        else
                        {
                            // Detect other types of roles
                            Role roleSECURITYADMIN;
                            Role roleUSERADMIN;
                            Role roleSYSADMIN;
                            Role roleACCOUNTADMIN;

                            if (rolesDict.TryGetValue("ACCOUNTADMIN", out roleACCOUNTADMIN) == true &&
                                rolesDict.TryGetValue("SECURITYADMIN", out roleSECURITYADMIN) == true &&
                                rolesDict.TryGetValue("USERADMIN", out roleUSERADMIN) && 
                                rolesDict.TryGetValue("SYSADMIN", out roleSYSADMIN))
                            {
                                // Check what we are rooted to
                                if ((role.RollsUpTo(roleUSERADMIN) == true || role.RollsUpTo(roleSECURITYADMIN) == true) && role.RollsUpTo(roleSYSADMIN) == false)
                                {
                                    role.Type = RoleType.RoleManagement;
                                }

                                // Check between Functional and Access

                                // Schemas first
                                if (role.Type == RoleType.Unknown && grantsToSchemaList != null)
                                {
                                    List<Grant> grantsToSchemaForThisRoleList = grantsToSchemaList.Where(
                                        g => g.GrantedTo == role.Name && 
                                        g.Privilege != "USAGE" && 
                                        g.Privilege != "OWNERSHIP" && 
                                        g.Privilege != "MONITOR").ToList();
                                    if (grantsToSchemaForThisRoleList != null && grantsToSchemaForThisRoleList.Count > 0)
                                    {
                                        role.Type = RoleType.Access;
                                    }
                                }

                                // Tables second, and only if the role type is still undetermined
                                if (role.Type == RoleType.Unknown && grantsToTableList != null)
                                {
                                    List<Grant> grantsToTableForThisRoleList = grantsToTableList.Where(
                                        g => g.GrantedTo == role.Name && 
                                        g.Privilege != "USAGE" && 
                                        g.Privilege != "OWNERSHIP" && 
                                        g.Privilege != "REFERENCES" && 
                                        g.Privilege != "REBUILD").ToList();
                                    if (grantsToTableForThisRoleList != null && grantsToTableForThisRoleList.Count > 0)
                                    {
                                        role.Type = RoleType.Access;
                                    }
                                }

                                // Views third, and only if the role type is still undetermined
                                if (role.Type == RoleType.Unknown && grantsToViewList != null)
                                {
                                    List<Grant> grantsToViewForThisRoleList = grantsToTableList.Where(
                                        g => g.GrantedTo == role.Name && 
                                        g.Privilege != "USAGE" && 
                                        g.Privilege != "OWNERSHIP" && 
                                        g.Privilege != "REFERENCES" && 
                                        g.Privilege != "REBUILD").ToList();
                                    if (grantsToViewForThisRoleList != null && grantsToViewForThisRoleList.Count > 0)
                                    {
                                        role.Type = RoleType.Access;
                                    }
                                }

                                // After comparing to schema, table and view, it is still unknown. Does it have any other roles below which would make it Functional?
                                if (role.Type == RoleType.Unknown && grantsToRoleList != null)
                                {
                                    List<Grant> grantsToRoleForThisRoleList = grantsToRoleList.Where(
                                        g => g.GrantedTo == role.Name && 
                                        g.Privilege == "USAGE").ToList();
                                    if (grantsToRoleForThisRoleList != null && grantsToRoleForThisRoleList.Count > 0)
                                    {
                                        role.Type = RoleType.Functional;
                                    }
                                }

                                if (role.Type == RoleType.Unknown && role.RollsUpTo(roleACCOUNTADMIN) == false)
                                {
                                    // This role is not connected to the proper hierarchy
                                    role.Type = RoleType.NotUnderAccountAdmin;
                                }

                                if (role.Type == RoleType.Functional && role.RollsUpTo(roleSYSADMIN) == false)
                                {
                                    // Functional but not in the right hierarchy
                                    role.Type = RoleType.FunctionalNotUnderSysadmin;
                                }

                                if (role.Type == RoleType.Access && role.RollsUpTo(roleSYSADMIN) == false)
                                {
                                    // Access but not in the right hierarchy
                                    role.Type = RoleType.AccessNotUnderSysadmin;
                                }
                            }
                        }
                    }

                    loggerConsole.Info("Building role ancestry paths");

                    // Now create role usage hiearchy
                    if (grantsToRolesList != null)
                    {
                        List<Grant> grantsToRolesUsageList = grantsToRolesList.Where(g => g.Privilege == "USAGE").ToList();
                        if (grantsToRolesUsageList != null)
                        {
                            List<RoleHierarchy> roleHierarchiesList = new List<RoleHierarchy>(grantsToRolesUsageList.Count);

                            j = 0;

                            // Build stuff for flow diagrams using the USAGE rights and the role hierarchy
                            foreach (Grant grant in grantsToRolesUsageList)
                            {
                                Role roleBeingGranted;

                                if (rolesDict.TryGetValue(grant.ObjectName, out roleBeingGranted) == true)
                                {
                                    RoleHierarchy roleHierarchy = new RoleHierarchy();
                                    roleHierarchy.Name = roleBeingGranted.Name;
                                    roleHierarchy.Type = roleBeingGranted.Type;
                                    roleHierarchy.AncestryPaths = roleBeingGranted.AncestryPaths;
                                    roleHierarchy.NumAncestryPaths = roleHierarchy.AncestryPaths.Split('\n').Count();
                                    roleHierarchy.GrantedTo = grant.GrantedTo;
                                    
                                    if (roleHierarchy.AncestryPaths.StartsWith("ACCOUNTADMIN", true, CultureInfo.InvariantCulture) == false)
                                    {
                                        // Everything should roll up to ACCOUNTADMIN
                                        // But when it doesn't, highlight this by pointing the highest unconnected role
                                        roleHierarchy.ImportantAncestor = String.Format("{0}", roleHierarchy.AncestryPaths.Split('\\')[0]);
                                    }
                                    else if (roleBeingGranted.Type == RoleType.Access || roleBeingGranted.Type == RoleType.Functional)
                                    {
                                        // Walk up to the last Functional role in the hierarchy going up
                                        bool keepGoing = true;
                                        Role currentRole = roleBeingGranted;
                                        while (keepGoing)
                                        {
                                            if (currentRole.ParentRoles.Count == 0)
                                            {
                                                keepGoing = false;
                                            }
                                            else
                                            {
                                                // only go up on the primary path
                                                Role parentRole = currentRole.ParentRoles[0];
                                                if (parentRole.Type == RoleType.Access || parentRole.Type == RoleType.Functional)
                                                {
                                                    currentRole = parentRole;
                                                }
                                                else
                                                {
                                                    keepGoing = false;
                                                }
                                            }
                                        }
                                        roleHierarchy.ImportantAncestor = currentRole.Name;
                                    }
                                    else 
                                    {
                                        // Default for all others to the root
                                        roleHierarchy.ImportantAncestor = "ACCOUNTADMIN";
                                    }

                                    roleHierarchiesList.Add(roleHierarchy);
                                }

                                j++;
                                if (j % 1000 == 0)
                                {
                                    Console.Write("{0}.", j);
                                }
                            }
                            Console.WriteLine("Done {0} items", grantsToRolesUsageList.Count);

                            // Now loop through the list of roles looking for the stragglers that have no other roles below them or aren't parented to any
                            foreach (Role role in rolesList)
                            {
                                if (roleHierarchiesList.Count(r => r.Name == role.Name) == 0 && roleHierarchiesList.Count(r => r.GrantedTo == role.Name) == 0)
                                {
                                    // Add this role explicily
                                    RoleHierarchy roleHierarchy = new RoleHierarchy();
                                    roleHierarchy.Name = role.Name;
                                    roleHierarchy.Type = role.Type;
                                    roleHierarchy.AncestryPaths = role.AncestryPaths;
                                    roleHierarchy.NumAncestryPaths = roleHierarchy.AncestryPaths.Split('\n').Count();
                                    roleHierarchy.GrantedTo = "<NOTHING>";
                                
                                    roleHierarchiesList.Add(roleHierarchy);
                                }
                            }
                            
                            // For each role, output the hierarchy records that relate to its parents and children
                            foreach (Role role in rolesList)
                            {
                                // Get list of Roles
                                List<Role> thisRoleAndItsRelationsNonUniqueList = new List<Role>(10);
                                thisRoleAndItsRelationsNonUniqueList.Add(role);
                                role.GetAllParentRoles(role, thisRoleAndItsRelationsNonUniqueList);
                                role.GetAllChildRoles(role, thisRoleAndItsRelationsNonUniqueList);
                                // Filter to only unique items
                                var thisRoleAndItsRelationsListGrouped = thisRoleAndItsRelationsNonUniqueList.GroupBy(r => r.Name);
                                List<Role> thisRoleAndItsRelationsList = new List<Role>(thisRoleAndItsRelationsListGrouped.Count());
                                foreach (var roleGroup in thisRoleAndItsRelationsListGrouped)
                                {
                                    thisRoleAndItsRelationsList.Add(roleGroup.First());
                                }

                                // Get hierarchy of roles
                                List<RoleHierarchy> thisRoleAndItsRelationsHierarchiesNonUniqueList = new List<RoleHierarchy>(100);
                                role.GetAllParentRoleHierarchies(role, thisRoleAndItsRelationsHierarchiesNonUniqueList);
                                role.GetAllChildRoleHierarchies(role, thisRoleAndItsRelationsHierarchiesNonUniqueList);
                                // Filter to only unique items
                                var thisRoleAndItsRelationsHierarchiesGrouped = thisRoleAndItsRelationsHierarchiesNonUniqueList.GroupBy(r => String.Format("{0}-{1}", r.Name, r.GrantedTo));
                                List<RoleHierarchy> thisRoleAndItsRelationsHierarchiesList = new List<RoleHierarchy>(thisRoleAndItsRelationsHierarchiesGrouped.Count());
                                foreach (var roleHierarchyGroup in thisRoleAndItsRelationsHierarchiesGrouped)
                                {
                                    thisRoleAndItsRelationsHierarchiesList.Add(roleHierarchyGroup.First());
                                }

                                thisRoleAndItsRelationsHierarchiesList = thisRoleAndItsRelationsHierarchiesList.OrderBy(r => r.Name).ThenBy(r => r.GrantedTo).ToList();

                                FileIOHelper.WriteListToCSVFile<Role>(thisRoleAndItsRelationsList, new RoleMap(), FilePathMap.Report_RoleDetail_RoleAndItsRelations_FilePath(role.Name));
                                FileIOHelper.WriteListToCSVFile<RoleHierarchy>(thisRoleAndItsRelationsHierarchiesList, new RoleHierarchyMap(), FilePathMap.Report_RoleHierarchy_RoleAndItsRelations_FilePath(role.Name));
                            }

                            roleHierarchiesList = roleHierarchiesList.OrderBy(r => r.Name).ThenBy(r => r.GrantedTo).ToList();
                            FileIOHelper.WriteListToCSVFile<RoleHierarchy>(roleHierarchiesList, new RoleHierarchyMap(), FilePathMap.Report_RoleHierarchy_FilePath());
                        }
                    }

                    rolesList = rolesList.OrderBy(r => r.Name).ToList();

                    FileIOHelper.WriteListToCSVFile<Role>(rolesList, new RoleMap(), FilePathMap.Report_RoleDetail_FilePath());
                    FileIOHelper.WriteListToCSVFile<Role>(rolesList, new RoleOwnerSankeyMap(), FilePathMap.Report_RoleOwnerSankey_FilePath(), false, false);                    
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
            return true;
        }
    }
}