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
    public class IndexGrantDifferences : JobStepBase
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

            ProgramOptions programOptionsLeft = new ProgramOptions();
            programOptionsLeft.ReportFolderPath = programOptions.LeftReportFolderPath;
            FilePathMap filePathMapLeft = new FilePathMap(programOptionsLeft);

            ProgramOptions programOptionsRight = new ProgramOptions();
            programOptionsRight.ReportFolderPath = programOptions.RightReportFolderPath;
            FilePathMap filePathMapRight = new FilePathMap(programOptionsRight);

            try
            {
                // Load all the grants from both sides
                loggerConsole.Info("Loading Grants for Left side {0}", programOptionsLeft.ReportFolderPath);
                List<Grant> grantsAllLeftList = FileIOHelper.ReadListFromCSVFile<Grant>(filePathMapLeft.Report_RoleGrant_FilePath(), new GrantMap());
                if (grantsAllLeftList == null || grantsAllLeftList.Count == 0)
                {
                    loggerConsole.Warn("No grants to compare on the Left side");
                    return false;
                }
                
                loggerConsole.Info("Loading Grants for Right side {0}", programOptionsRight.ReportFolderPath);
                List<Grant> grantsAllRightList = FileIOHelper.ReadListFromCSVFile<Grant>(filePathMapRight.Report_RoleGrant_FilePath(), new GrantMap());
                if (grantsAllRightList == null || grantsAllRightList.Count == 0)
                {
                    loggerConsole.Warn("No grants to compare with on the Right side");
                    return false;
                }

                // If we got here, we potentially have a large list of Grants on both sides all in memory, begin comparison
                logger.Trace("Left side list of Grants {0} items", grantsAllLeftList.Count);
                logger.Trace("Right side list of Grants {0} items", grantsAllRightList.Count);
                loggerConsole.Info("Left side {0} grants <-> Right side {1} Grants", grantsAllLeftList.Count, grantsAllRightList.Count);

                // Avoid duplicate grants by Grouping first
                // Really could only happen when converting from the spreadsheets, not with any dumps from Snowflake
                Dictionary<string, Grant> grantsAllLeftDict = new Dictionary<string, Grant>(grantsAllLeftList.Count);
                foreach (Grant grant in grantsAllLeftList)
                {
                    if (grantsAllLeftDict.ContainsKey(grant.UniqueIdentifier) == false)
                    {
                        grantsAllLeftDict.Add(grant.UniqueIdentifier, grant);
                    }
                }
                Dictionary<string, Grant> grantsAllRightDict = new Dictionary<string, Grant>(grantsAllRightList.Count);
                foreach (Grant grant in grantsAllRightList)
                {
                    if (grantsAllRightDict.ContainsKey(grant.UniqueIdentifier) == false)
                    {
                        grantsAllRightDict.Add(grant.UniqueIdentifier, grant);
                    }
                }

                // Here is what we get in the reference and difference lists
                // List             List
                // Reference        Difference      Action
                // AAA              AAA             Compare items
                // BBB                              item in Difference is MISSING
                //                  CCC             item in Difference is EXTRA
                // The columns of Grants are:
                //      Privilege,ObjectType,ObjectName,GrantedTo,DBName,SchemaName,EntityName,GrantedBy,WithGrantOption,CreatedOn,CreatedOnUTC
                // Out of those, the identifying combination of a Grant is:
                //      Privilege,ObjectType,ObjectName,GrantedTo
                // And it is stored in Grant.UniqueIdentifier

                // Assume 1% differences
                List<GrantDifference> grantDifferencesList = new List<GrantDifference>(grantsAllLeftList.Count / 100);

                loggerConsole.Info("Comparing Left side -> Right Side");
                int j = 0;

                // First loop through Reference list looking for matches
                foreach (Grant grantLeft in grantsAllLeftDict.Values)
                {
                    Grant grantRight = null;
                    if (grantsAllRightDict.TryGetValue(grantLeft.UniqueIdentifier, out grantRight) == true)
                    {
                        // Found matching entity AAA. Let's compare them against each other                        
                        List<string> differentPropertiesList = new List<string>(2);

                        // Only compare if GrantedBy is non-empty
                        if ((grantLeft.GrantedBy.Length > 0 && grantRight.GrantedBy.Length > 0) && grantLeft.GrantedBy != grantRight.GrantedBy)
                        {
                            differentPropertiesList.Add("GrantedBy");
                        }
                        
                        // Only compare of CreatedOn is a real date
                        if ((grantLeft.CreatedOn != DateTime.MinValue && grantRight.CreatedOn != DateTime.MinValue) && grantLeft.CreatedOn != grantRight.CreatedOn)
                        {
                            // Sometimes the CreatedOn is only different just a tiny little bit like here:
                            // CreatedOnUTCLeft	                CreatedOnUTCRight
                            // 2020-12-01T01:12:16.1360000Z     2020-12-01T01:12:16.3940000Z
                            // As you can see it is different only in milliseconds. Must be an FDB thing
                            // Going to ignore sub-second differences
                            TimeSpan timeDifference = grantLeft.CreatedOn - grantRight.CreatedOn;
                            if (Math.Abs(timeDifference.TotalSeconds) > 1)
                            {
                                differentPropertiesList.Add("CreatedOn");
                            }                            
                        }
                        if (grantLeft.WithGrantOption != grantRight.WithGrantOption)
                        {
                            differentPropertiesList.Add("WithGrantOption");
                        }

                        if (differentPropertiesList.Count > 0)
                        {
                            GrantDifference grantDifference = new GrantDifference();
                            grantDifference.UniqueIdentifier = grantRight.UniqueIdentifier;
                            grantDifference.Privilege = grantRight.Privilege;
                            grantDifference.ObjectType = grantRight.ObjectType;
                            grantDifference.ObjectName = grantRight.ObjectName;
                            grantDifference.GrantedTo = grantRight.GrantedTo;
                            grantDifference.DBName = grantRight.DBName;
                            grantDifference.SchemaName = grantRight.SchemaName;
                            grantDifference.EntityName = grantRight.EntityName;

                            grantDifference.ReportLeft = programOptions.LeftReportFolderPath;
                            grantDifference.ReportRight = programOptions.RightReportFolderPath;
                            grantDifference.Difference = DIFFERENCE_DIFFERENT;
                            grantDifference.DifferenceDetails = String.Join(',', differentPropertiesList.ToArray());

                            grantDifference.GrantedByLeft = grantLeft.GrantedBy;
                            grantDifference.CreatedOnUTCLeft = grantLeft.CreatedOnUTC;
                            grantDifference.WithGrantOptionLeft = grantLeft.WithGrantOption;
                            grantDifference.GrantedByRight = grantRight.GrantedBy;
                            grantDifference.CreatedOnUTCRight = grantRight.CreatedOnUTC;
                            grantDifference.WithGrantOptionRight = grantRight.WithGrantOption;

                            grantDifferencesList.Add(grantDifference);
                        }

                        // Remove this object as already considered
                        grantsAllRightDict[grantRight.UniqueIdentifier] = null;
                    }
                    else
                    {
                        // No match. This must be entity BBB, where item in Difference is MISSING
                        GrantDifference grantDifference = new GrantDifference();
                        grantDifference.UniqueIdentifier = grantLeft.UniqueIdentifier;
                        grantDifference.Privilege = grantLeft.Privilege;
                        grantDifference.ObjectType = grantLeft.ObjectType;
                        grantDifference.ObjectName = grantLeft.ObjectName;
                        grantDifference.GrantedTo = grantLeft.GrantedTo;
                        grantDifference.DBName = grantLeft.DBName;
                        grantDifference.SchemaName = grantLeft.SchemaName;
                        grantDifference.EntityName = grantLeft.EntityName;
                        
                        grantDifference.ReportLeft = programOptions.LeftReportFolderPath;
                        grantDifference.ReportRight = programOptions.RightReportFolderPath;
                        grantDifference.Difference = DIFFERENCE_MISSING;
                        grantDifference.DifferenceDetails = PROPERTY_ENTIRE_OBJECT;

                        grantDifference.GrantedByLeft = grantLeft.GrantedBy;
                        grantDifference.CreatedOnUTCLeft = grantLeft.CreatedOnUTC;
                        grantDifference.WithGrantOptionLeft = grantLeft.WithGrantOption;

                        grantDifferencesList.Add(grantDifference);
                    }

                    // Remove this object as already considered
                    grantsAllLeftDict[grantLeft.UniqueIdentifier] = null;

                    j++;
                    if (j % 1000 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }

                Console.WriteLine("Processed {0} comparisons", grantsAllLeftDict.Count);

                loggerConsole.Info("Comparing Right side -> Left Side");
                j = 0;
                foreach (Grant grantRight in grantsAllRightDict.Values)
                {
                    if (grantRight != null)
                    {
                        GrantDifference grantDifference = new GrantDifference();
                        grantDifference.UniqueIdentifier = grantRight.UniqueIdentifier;
                        grantDifference.Privilege = grantRight.Privilege;
                        grantDifference.ObjectType = grantRight.ObjectType;
                        grantDifference.ObjectName = grantRight.ObjectName;
                        grantDifference.GrantedTo = grantRight.GrantedTo;
                        grantDifference.DBName = grantRight.DBName;
                        grantDifference.SchemaName = grantRight.SchemaName;
                        grantDifference.EntityName = grantRight.EntityName;

                        grantDifference.ReportLeft = programOptions.LeftReportFolderPath;
                        grantDifference.ReportRight = programOptions.RightReportFolderPath;
                        grantDifference.Difference = DIFFERENCE_EXTRA;
                        grantDifference.DifferenceDetails = PROPERTY_ENTIRE_OBJECT;

                        grantDifference.GrantedByRight = grantRight.GrantedBy;
                        grantDifference.CreatedOnUTCRight = grantRight.CreatedOnUTC;
                        grantDifference.WithGrantOptionRight = grantRight.WithGrantOption;

                        grantDifferencesList.Add(grantDifference);
                    }

                    j++;
                    if (j % 1000 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }

                loggerConsole.Info("Found {0} differences", grantDifferencesList.Count);

                FileIOHelper.WriteListToCSVFile<GrantDifference>(grantDifferencesList, new GrantDifferenceMap(), FilePathMap.Report_RoleGrant_Differences_FilePath());

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