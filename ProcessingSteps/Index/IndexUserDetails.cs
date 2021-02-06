using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class IndexUserDetails : JobStepBase
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
                FileIOHelper.CreateFolder(this.FilePathMap.Report_User_FolderPath());

                List<User> usersList = FileIOHelper.ReadListFromCSVFile<User>(FilePathMap.Data_ShowUsers_FilePath(), new UserShowUsersMap());
                if (usersList != null)
                {
                    loggerConsole.Info("Parsing user details for {0} users", usersList.Count);

                    int j = 0;

                    foreach(User user in usersList)
                    {
                        logger.Trace("Parsing details for user {0}", user.LOGIN_NAME);

                        List<UserProperty> userPropertiesList = FileIOHelper.ReadListFromCSVFile<UserProperty>(FilePathMap.Data_DescribeUser_FilePath(user.NAME), new UserPropertyMap());
                        if (userPropertiesList != null)
                        {
                            foreach (UserProperty userProperty in userPropertiesList)
                            {
                                if (userProperty.PropValue == "null") userProperty.PropValue = String.Empty;
                                switch (userProperty.PropName)
                                {
                                    case "COMMENT":
                                        user.COMMENT = userProperty.PropValue;
                                        break;
                                    case "DAYS_TO_EXPIRY":
                                        if (userProperty.PropValue != null && userProperty.PropValue.Length > 0) try { user.DAYS_TO_EXPIRY = Convert.ToInt32(userProperty.PropValue); } catch {}
                                        break;
                                    case "DEFAULT_NAMESPACE":
                                        user.DEFAULT_NAMESPACE = userProperty.PropValue;
                                        break;
                                    case "DEFAULT_ROLE":
                                        user.DEFAULT_ROLE = userProperty.PropValue;
                                        break;
                                    case "DEFAULT_WAREHOUSE":
                                        user.DEFAULT_WAREHOUSE = userProperty.PropValue;
                                        break;
                                    case "DISABLED":
                                        user.DISABLED = Convert.ToBoolean(userProperty.PropValue);
                                        break;
                                    case "DISPLAY_NAME":
                                        user.DISPLAY_NAME = userProperty.PropValue;
                                        break;
                                    case "EMAIL":
                                        user.EMAIL = userProperty.PropValue;
                                        break;
                                    case "EXT_AUTHN_DUO":
                                        user.EXT_AUTHN_DUO = Convert.ToBoolean(userProperty.PropValue);
                                        break;
                                    case "EXT_AUTHN_UID":
                                        user.EXT_AUTHN_UID = userProperty.PropValue;
                                        break;
                                    case "FIRST_NAME":
                                        user.FIRST_NAME = userProperty.PropValue;
                                        break;
                                    case "LAST_NAME":
                                        user.LAST_NAME = userProperty.PropValue;
                                        break;
                                    case "LOGIN_NAME":
                                        // Already parsed
                                        break;
                                    case "MIDDLE_NAME":
                                        user.MIDDLE_NAME = userProperty.PropValue;
                                        break;
                                    case "MINS_TO_BYPASS_MFA":
                                        if (userProperty.PropValue != null && userProperty.PropValue.Length > 0) try { user.MINS_TO_BYPASS_MFA = Convert.ToInt32(userProperty.PropValue); } catch {}
                                        break;
                                    case "MINS_TO_BYPASS_NETWORK_POLICY":
                                        if (userProperty.PropValue != null && userProperty.PropValue.Length > 0) try { user.MINS_TO_BYPASS_NETWORK_POLICY = Convert.ToInt32(userProperty.PropValue); } catch {}
                                        break;
                                    case "MINS_TO_UNLOCK":
                                        if (userProperty.PropValue != null && userProperty.PropValue.Length > 0) try { user.MINS_TO_UNLOCK = Convert.ToInt32(userProperty.PropValue); } catch {}
                                        break;
                                    case "MUST_CHANGE_PASSWORD":
                                        user.MUST_CHANGE_PASSWORD = Convert.ToBoolean(userProperty.PropValue);
                                        break;
                                    case "NAME":
                                        // Already parsed, but check for special characters
                                        if (String.Compare(userProperty.PropValue, quoteObjectIdentifier(userProperty.PropValue), true, CultureInfo.InvariantCulture) == 0)
                                        {
                                            user.IsObjectIdentifierSpecialCharacters = false;
                                        }
                                        else
                                        {
                                            user.IsObjectIdentifierSpecialCharacters = true;
                                        }
                                        break;
                                    case "PASSWORD":
                                        // Not parsing
                                        break;
                                    case "PASSWORD_LAST_SET_TIME":
                                        if (userProperty.PropValue != null && userProperty.PropValue.Length > 0) 
                                        try 
                                        { 
                                            // https://community.snowflake.com/s/article/4-26-Release-Notes-July-28-30-2020
                                            // The timestamp on which the last non-null password was set for the user. 
                                            // Default to null if no password has been set yet. 
                                            // Values of 292278994-08-17 07:12:55.807 or 1969-12-31 23:59:59.999 indicate the password was set before the inclusion of this row. A value of 1969-12-31 23:59:59.999 can also indicate an expired password and the user needs to change their password.
                                            if (userProperty.PropValue == "292278994-08-17 07:12:55.807")
                                            {
                                                // This date 300 million year in the future is not parseable
                                                // Set it back to good old 1969
                                                userProperty.PropValue = "1969-12-31 23:59:59.999";
                                            }
                                            user.PASSWORD_LAST_SET_TIME = Convert.ToDateTime(userProperty.PropValue); 
                                        } 
                                        catch {}
                                        break;
                                    case "RSA_PUBLIC_KEY_2_FP":
                                        user.RSA_PUBLIC_KEY_2_FP = userProperty.PropValue;
                                        break;
                                    case "RSA_PUBLIC_KEY_FP":
                                        user.RSA_PUBLIC_KEY_FP = userProperty.PropValue;
                                        break;
                                    case "SNOWFLAKE_LOCK":
                                        user.SNOWFLAKE_LOCK = Convert.ToBoolean(userProperty.PropValue);
                                        break;
                                    case "SNOWFLAKE_SUPPORT":
                                        user.SNOWFLAKE_SUPPORT = Convert.ToBoolean(userProperty.PropValue);
                                        break;
                                    default:
                                        logger.Warn("Unknown user property {0} for user {1}", userProperty, user);
                                        break;
                                }
                            }
                        }
                        
                        j++;
                        if (j % 10 == 0)
                        {
                            Console.Write("{0}.", j);
                        }
                    }
                    Console.WriteLine("Done {0} items", usersList.Count);

                    FileIOHelper.WriteListToCSVFile<User>(usersList, new UserDetailsMap(), FilePathMap.Report_UserDetail_FilePath());                    
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
    }
}