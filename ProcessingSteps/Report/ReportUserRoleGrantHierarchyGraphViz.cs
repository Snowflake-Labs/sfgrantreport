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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{
    public class ReportUserRoleGrantHierarchyGraphViz : JobStepBase
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
                FileIOHelper.CreateFolder(this.FilePathMap.Report_GraphViz_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Diagram_SVG_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Diagram_PNG_FolderPath());
                FileIOHelper.CreateFolder(this.FilePathMap.Report_Diagram_PDF_FolderPath());

                #region Construct object hierarchy with grants

                Account account = buildObjectHierarchyWithGrants();

                #endregion

                List<Role> rolesList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Report_RoleDetail_FilePath(), new RoleMap());
                if (rolesList != null)
                {
                    #region Make GraphViz charts

                    loggerConsole.Info("Creating visualizations for {0} roles", rolesList.Count);

                    Role syntheticRoleAll = new Role();
                    syntheticRoleAll.Name = "ALL_ROLES_TOGETHER_SYNTHETIC";
                    rolesList.Insert(0, syntheticRoleAll);

                    ParallelOptions parallelOptions = new ParallelOptions();
                    if (programOptions.ProcessSequentially == true)
                    {
                        parallelOptions.MaxDegreeOfParallelism = 1;
                    }

                    int j = 0;

                    Parallel.ForEach<Role, int>(
                        rolesList,
                        parallelOptions,
                        () => 0,
                        (role, loop, subtotal) =>
                        {
                            logger.Info("Processing visualization for {0}", role);  

                            List<RoleHierarchy> thisRoleAndItsRelationsHierarchiesList = FileIOHelper.ReadListFromCSVFile<RoleHierarchy>(FilePathMap.Report_RoleHierarchy_RoleAndItsRelations_FilePath(role.Name), new RoleHierarchyMap());;
                            List<Role> thisRoleAndItsRelationsList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Report_RoleDetail_RoleAndItsRelations_FilePath(role.Name), new RoleMap());

                            if (role == syntheticRoleAll)
                            {
                                thisRoleAndItsRelationsHierarchiesList = FileIOHelper.ReadListFromCSVFile<RoleHierarchy>(FilePathMap.Report_RoleHierarchy_FilePath(), new RoleHierarchyMap());;
                                thisRoleAndItsRelationsList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Report_RoleDetail_FilePath(), new RoleMap());
                            }

                            if (thisRoleAndItsRelationsList != null && thisRoleAndItsRelationsHierarchiesList != null)
                            {
                                Dictionary<string, Role> rolesDict = thisRoleAndItsRelationsList.ToDictionary(k => k.Name, r => r);
                                Dictionary<string, Role> roleNamesOutput = new Dictionary<string, Role>(thisRoleAndItsRelationsList.Count);
                                Role roleBeingOutput = null;

                                StringBuilder sbGraphViz = new StringBuilder(64 * thisRoleAndItsRelationsHierarchiesList.Count + 128);

                                // Start the graph and set its default settings
                                sbGraphViz.AppendLine("digraph {");
                                sbGraphViz.AppendLine(" layout=\"dot\";");
                                sbGraphViz.AppendLine(" rankdir=\"TB\";");
                                sbGraphViz.AppendLine(" center=true;");
                                sbGraphViz.AppendLine(" splines=\"ortho\";");
                                sbGraphViz.AppendLine(" overlap=false;");
                                //sbGraphViz.AppendLine(" colorscheme=\"SVG\";");
                                sbGraphViz.AppendLine(" node [shape=\"rect\" style=\"filled,rounded\" fontname=\"Courier New\"];");
                                sbGraphViz.AppendLine(" edge [fontname=\"Courier New\"];");

                                sbGraphViz.AppendFormat(" // Graph for the Role {0}", role); sbGraphViz.AppendLine();

                                #region Role boxes

                                // Role boxes
                                sbGraphViz.AppendLine();
                                sbGraphViz.AppendLine(" // Roles");
                                sbGraphViz.AppendLine  ("  subgraph cluster_roles {");
                                sbGraphViz.AppendFormat("   label = \"roles related to: {0}\";", role); sbGraphViz.AppendLine();

                                // Add the role itself
                                sbGraphViz.AppendFormat("  \"{0}\"{1};", role.Name.Replace("\"", "\\\""), getRoleStyleAttribute(role)); sbGraphViz.AppendLine();
                                roleNamesOutput.Add(role.Name, role);

                                foreach (RoleHierarchy roleHierarchy in thisRoleAndItsRelationsHierarchiesList)
                                {
                                    if (roleHierarchy.GrantedTo != "<NOTHING>" && roleNamesOutput.ContainsKey(roleHierarchy.GrantedTo) == false)
                                    {
                                        // Name of the role with color
                                        rolesDict.TryGetValue(roleHierarchy.GrantedTo, out roleBeingOutput);
                                        sbGraphViz.AppendFormat("  \"{0}\"{1};", roleHierarchy.GrantedTo.Replace("\"", "\\\""), getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                                        roleNamesOutput.Add(roleHierarchy.GrantedTo, roleBeingOutput);
                                    }
                                    
                                    if (roleNamesOutput.ContainsKey(roleHierarchy.Name) == false)
                                    {
                                        // Name of the role with color
                                        rolesDict.TryGetValue(roleHierarchy.Name, out roleBeingOutput);
                                        sbGraphViz.AppendFormat("  \"{0}\"{1};", roleHierarchy.Name.Replace("\"", "\\\""), getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                                        roleNamesOutput.Add(roleHierarchy.Name, roleBeingOutput);
                                    }
                                }
                                sbGraphViz.AppendLine("  }// /Roles");

                                #endregion

                                #region Role hierachy

                                // Role connections
                                sbGraphViz.AppendLine();
                                sbGraphViz.AppendLine(" // Role hierarchy");
                                foreach (RoleHierarchy roleHierarchy in thisRoleAndItsRelationsHierarchiesList)
                                {
                                    if (roleHierarchy.GrantedTo == "<NOTHING>") continue;

                                    // Role to role connector
                                    sbGraphViz.AppendFormat(" \"{0}\"->\"{1}\";", roleHierarchy.GrantedTo.Replace("\"", "\\\""), roleHierarchy.Name.Replace("\"", "\\\"")); sbGraphViz.AppendLine();
                                }
                                sbGraphViz.AppendLine(" // /Role hierarchy");

                                #endregion

                                if (role != syntheticRoleAll)
                                {
                                    #region Databases, Schemas, Tables and Views

                                    sbGraphViz.AppendLine();
                                    sbGraphViz.AppendLine(" // Databases");
                                    sbGraphViz.AppendLine(" subgraph cluster_db_wrapper {");
                                    sbGraphViz.AppendLine("  label = \"Databases\";");
                                    sbGraphViz.AppendLine();

                                    int databaseIndex = 0;
                                    foreach (Database database in account.Databases)
                                    {
                                        // Should output database
                                        bool isDatabaseRelatedToSelectedRole = false;
                                        foreach (Grant grant in database.Grants)
                                        {
                                            if (grant.Privilege == "USAGE" || grant.Privilege == "OWNERSHIP")
                                            {
                                                if (roleNamesOutput.ContainsKey(grant.GrantedTo) == true)
                                                {
                                                    isDatabaseRelatedToSelectedRole = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (isDatabaseRelatedToSelectedRole == false) continue;

                                        // Output database
                                        sbGraphViz.AppendFormat("  // Database {0}", database.FullName); sbGraphViz.AppendLine();
                                        sbGraphViz.AppendFormat("  subgraph cluster_db_{0} {{", databaseIndex); sbGraphViz.AppendLine();
                                        sbGraphViz.AppendLine  ("   style=\"filled\";");
                                        sbGraphViz.AppendLine  ("   fillcolor=\"snow\";");
                                        sbGraphViz.AppendFormat("   label = \"db: {0}\";", database.ShortName); sbGraphViz.AppendLine();
                                        sbGraphViz.AppendLine  ("   node [shape=\"cylinder\" fillcolor=\"darkkhaki\"];");
                                        sbGraphViz.AppendLine();

                                        sbGraphViz.AppendFormat("   \"{0}\";", database.FullName); sbGraphViz.AppendLine();
                                        sbGraphViz.AppendLine();

                                        // List of schemas with number of tables and views
                                        sbGraphViz.AppendFormat("   \"{0}.schema\"  [shape=\"folder\" label=<", database.FullName);  sbGraphViz.AppendLine();
                                        sbGraphViz.AppendLine  ("    <table border=\"0\" cellborder=\"1\" bgcolor=\"white\">");
                                        sbGraphViz.AppendLine  ("     <tr><td>S</td><td>T</td><td>V</td></tr>");
                                        
                                        int schemaLimit = 0;
                                        foreach (Schema schema in database.Schemas)
                                        {
                                            // Only output 
                                            if (schemaLimit >= 10) 
                                            {
                                                sbGraphViz.AppendFormat("     <tr><td align=\"left\">Up to {0}</td><td align=\"right\">...</td><td align=\"right\">...</td></tr>", database.Schemas.Count); sbGraphViz.AppendLine();

                                                break;
                                            }

                                            // Do not output future grants which are in form of <SCHEMANAME>
                                            if (schema.ShortName.StartsWith("<") && schema.ShortName.EndsWith(">")) continue;

                                            sbGraphViz.AppendFormat("     <tr><td align=\"left\">{0}</td><td align=\"right\">{1}</td><td align=\"right\">{2}</td></tr>", System.Web.HttpUtility.HtmlEncode(schema.ShortName), schema.Tables.Count, schema.Views.Count); sbGraphViz.AppendLine();

                                            schemaLimit++;
                                        }
                                        sbGraphViz.AppendLine  ("    </table>>];");

                                        // Connect database to schemas
                                        sbGraphViz.AppendFormat("   \"{0}\"->\"{0}.schema\" [style=\"invis\"];", database.FullName); sbGraphViz.AppendLine();

                                        sbGraphViz.AppendFormat("  }} // /Database {0}", database.FullName); sbGraphViz.AppendLine();

                                        databaseIndex++;
                                    }

                                    sbGraphViz.AppendLine(" } // /Databases");

                                    #endregion

                                    #region Roles using databases 
                                    
                                    sbGraphViz.AppendLine();
                                    sbGraphViz.AppendLine(" // Roles using databases");

                                    // Output connectors from roles USAGE'ing databases
                                    foreach (Database database in account.Databases)
                                    {
                                        foreach (Grant grant in database.Grants)
                                        {
                                            if (grant.Privilege == "USAGE" || grant.Privilege == "OWNERSHIP")
                                            {
                                                if (roleNamesOutput.ContainsKey(grant.GrantedTo) == true)
                                                {
                                                    sbGraphViz.AppendFormat(" \"{0}\"->\"{1}\" [color=\"darkkhaki\"];", grant.GrantedTo, grant.ObjectNameUnquoted); sbGraphViz.AppendLine();
                                                }
                                            }
                                        }
                                    }

                                    sbGraphViz.AppendLine(" // /Roles using databases");

                                    #endregion
                                }

                                #region Legend

                                // Output Legend
                                sbGraphViz.AppendLine();
                                string legend = @" // Legend
    ""legend"" [label=<
    <table border=""0"" cellborder=""0"" bgcolor=""white"">
    <tr><td align=""center"">Legend</td></tr>
    <tr><td align=""left"" bgcolor=""lightgray"">BUILT IN</td></tr>
    <tr><td align=""left"" bgcolor=""beige"">SCIM</td></tr>
    <tr><td align=""left"" bgcolor=""palegreen"">ROLE MANAGEMENT</td></tr>
    <tr><td align=""left"" bgcolor=""orchid"">FUNCTIONAL</td></tr>
    <tr><td align=""left"" bgcolor=""plum"">FUNCTIONAL, NOT UNDER SYSADMIN</td></tr>
    <tr><td align=""left"" bgcolor=""lightblue"">ACCESS</td></tr>
    <tr><td align=""left"" bgcolor=""azure"">ACCESS, NOT UNDER SYSADMIN</td></tr>
    <tr><td align=""left"" bgcolor=""navajowhite"">UNKNOWN</td></tr>
    <tr><td align=""left"" bgcolor=""orange"">UNKNOWN, NOT UNDER ACCOUNTADMIN</td></tr>
    </table>>];";
                                sbGraphViz.AppendLine(legend);
                                
                                #endregion

                                // Close the graph
                                sbGraphViz.AppendLine("}");
        
                                FileIOHelper.SaveFileToPath(sbGraphViz.ToString(), FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name), false);
                            }

                            return 1;
                        },
                        (finalResult) =>
                        {
                            Interlocked.Add(ref j, finalResult);
                            if (j % 50 == 0)
                            {
                                Console.Write("[{0}].", j);
                            }
                        }
                    );
                    loggerConsole.Info("Completed {0} Roles", rolesList.Count);

                    #endregion

                    #region HTML file with Links to Files

                    loggerConsole.Info("Creating HTML links for {0} roles", rolesList.Count);

                    // Create the HTML page with links for all the images
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.OmitXmlDeclaration = true;
                    xmlWriterSettings.Indent = true;

                    using (XmlWriter xmlWriter = XmlWriter.Create(FilePathMap.UsersRolesAndGrantsWebReportFilePath(), xmlWriterSettings))
                    {
                        xmlWriter.WriteDocType("html", null, null, null);
                        
                        xmlWriter.WriteStartElement("html");

                        xmlWriter.WriteStartElement("head");
                        xmlWriter.WriteStartElement("title");
                        xmlWriter.WriteString(String.Format("Snowflake Grants Report {0} {1} roles", programOptions.ReportJob.Connection, rolesList.Count));
                        xmlWriter.WriteEndElement(); // </title>
                        xmlWriter.WriteEndElement(); // </head>

                        xmlWriter.WriteStartElement("body");

                        xmlWriter.WriteStartElement("table");
                        xmlWriter.WriteAttributeString("border", "1");

                        // Header row
                        xmlWriter.WriteStartElement("tr");
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("Role"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("Type"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("# Parents"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("# Children"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("# Ancestry Paths"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("Online"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("SVG"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("PNG"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("th"); xmlWriter.WriteString("PDF"); xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement(); // </tr>
                        
                        foreach (Role role in rolesList)
                        {
                            xmlWriter.WriteStartElement("tr");
                            xmlWriter.WriteStartElement("td"); xmlWriter.WriteString(role.Name); xmlWriter.WriteEndElement();
                            xmlWriter.WriteStartElement("td"); xmlWriter.WriteString(role.Type.ToString()); xmlWriter.WriteEndElement();
                            xmlWriter.WriteStartElement("td"); xmlWriter.WriteString(role.NumParentRoles.ToString()); xmlWriter.WriteEndElement();
                            xmlWriter.WriteStartElement("td"); xmlWriter.WriteString(role.NumChildRoles.ToString()); xmlWriter.WriteEndElement();
                            xmlWriter.WriteStartElement("td"); xmlWriter.WriteString(role.NumAncestryPaths.ToString()); xmlWriter.WriteEndElement();

                            string graphText = FileIOHelper.ReadFileFromPath(FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name));

                            xmlWriter.WriteStartElement("td"); 
                            
                            // https://edotor.net
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", String.Format("https://edotor.net/?#{0}", Uri.EscapeDataString(graphText)));
                            xmlWriter.WriteString("Online");
                            xmlWriter.WriteEndElement(); // </a>

                            // // http://magjac.com/graphviz-visual-editor
                            // xmlWriter.WriteStartElement("a"); 
                            // xmlWriter.WriteAttributeString("href", String.Format("http://magjac.com/graphviz-visual-editor/?dot={0}", Uri.EscapeDataString(graphText)));
                            // xmlWriter.WriteString("Opt2");
                            // xmlWriter.WriteEndElement(); // </a>

                            // // https://stamm-wilbrandt.de/GraphvizFiddle/2.1.2/index.html
                            // xmlWriter.WriteStartElement("a"); 
                            // xmlWriter.WriteAttributeString("href", String.Format("https://stamm-wilbrandt.de/GraphvizFiddle/2.1.2/index.html?#{0}", Uri.EscapeDataString(graphText)));
                            // xmlWriter.WriteString("Opt3");
                            // xmlWriter.WriteEndElement(); // </a>

                            // // https://dreampuf.github.io/GraphvizOnline
                            // xmlWriter.WriteStartElement("a"); 
                            // xmlWriter.WriteAttributeString("href", String.Format("https://dreampuf.github.io/GraphvizOnline/#{0}", Uri.EscapeDataString(graphText)));
                            // xmlWriter.WriteString("Opt4");
                            // xmlWriter.WriteEndElement(); // </a>

                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_SVG_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("SVG");
                            xmlWriter.WriteEndElement(); // </a>
                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_PNG_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("PNG");
                            xmlWriter.WriteEndElement(); // </a>
                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_PDF_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("PDF");
                            xmlWriter.WriteEndElement(); // </a>
                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteEndElement(); // </tr>
                        }
                        xmlWriter.WriteEndElement(); // </table>

                        xmlWriter.WriteEndElement(); // </body>
                        xmlWriter.WriteEndElement(); // </html>
                    }

                    #endregion

                    #region Make SVG, PNG and PDF Files with GraphViz binaries

                    loggerConsole.Info("Making picture files for {0} roles", rolesList.Count);

                    GraphVizDriver graphVizDriver = new GraphVizDriver();
                    graphVizDriver.ValidateToolInstalled(programOptions);

                    if (graphVizDriver.ExecutableFilePath.Length > 0)
                    {
                        j = 0;

                        Parallel.ForEach<Role, int>(
                            rolesList,
                            parallelOptions,
                            () => 0,
                            (role, loop, subtotal) =>
                            {
                                loggerConsole.Info("Rendering graphs for {0}", role);  

                                graphVizDriver.ConvertGraphVizToFile(
                                    FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name),
                                    FilePathMap.Report_Diagram_SVG_RoleAndItsRelationsGrants_FilePath(role.Name, true), 
                                    "svg");
                                graphVizDriver.ConvertGraphVizToFile(
                                    FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name),
                                    FilePathMap.Report_Diagram_PNG_RoleAndItsRelationsGrants_FilePath(role.Name, true), 
                                    "png");
                                graphVizDriver.ConvertGraphVizToFile(
                                    FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name),
                                    FilePathMap.Report_Diagram_PDF_RoleAndItsRelationsGrants_FilePath(role.Name, true), 
                                    "pdf");                                

                                return 1;
                            },
                            (finalResult) =>
                            {
                                Interlocked.Add(ref j, finalResult);
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                        );
                        loggerConsole.Info("Completed {0} Roles", rolesList.Count);
                    }

                    #endregion
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

        private static string getRoleStyleAttribute(Role role)
        {
            string styleAttribute = String.Empty;;
            if (role == null) return styleAttribute ;
            
            // Color names being used here https://graphviz.org/doc/info/colors.html
            switch (role.Type)
            {
                case RoleType.BuiltIn:
                    styleAttribute = " [fillcolor=\"lightgray\"]";
                    break;
                case RoleType.SCIM:
                    styleAttribute = " [fillcolor=\"beige\"]";
                    break;
                case RoleType.RoleManagement:
                    styleAttribute = " [fillcolor=\"palegreen\"]";
                    break;
                case RoleType.Functional:
                    styleAttribute = " [fillcolor=\"orchid\"]";
                    break;
                case RoleType.FunctionalNotUnderSysadmin:
                    styleAttribute = " [fillcolor=\"plum\"]";
                    break;
                case RoleType.Access:
                    styleAttribute = " [fillcolor=\"lightblue\"]";
                    break;
                case RoleType.AccessNotUnderSysadmin:
                    styleAttribute = " [fillcolor=\"azure\"]";
                    break;
                case RoleType.Unknown:
                    styleAttribute = " [fillcolor=\"navajowhite\"]";
                    break;
                case RoleType.UnknownNotUnderAccountAdmin:
                    styleAttribute = " [fillcolor=\"orange\"]";
                    break;
                default:
                    break;
            }

            return styleAttribute;
        }

        private static string getDBName(string objectName)
        {
            return getObjectNamePart(objectName, 0).Trim('"');
        }

        private static string getSchemaName(string objectName)
        {
            return getObjectNamePart(objectName, 1).Trim('"');
        }

        private static string getObjectInSchemaName(string objectName)
        {
            return getObjectNamePart(objectName, 2).Trim('"');
        }

        private static string getObjectNamePart(string objectName, int indexOfPart)
        {
            string[] nameParts = objectName.Split('.');
            switch (indexOfPart)
            {
                case 0:
                    if (nameParts.Length == 0)
                    {
                        return objectName;
                    }
                    else
                    {
                        return nameParts[0];
                    }

                case 1:
                    if (nameParts.Length == 0 || nameParts.Length == 1)
                    {
                        return objectName;
                    }
                    else
                    {
                        return nameParts[1];
                    }

                case 2:
                    if (nameParts.Length == 0 || nameParts.Length == 1 || nameParts.Length == 2)
                    {
                        return objectName;
                    }
                    else
                    {
                        return nameParts[2];
                    }

                default:
                    throw new ArgumentException(String.Format("Invalid index of {0} for the full object name part for {1}", indexOfPart, objectName));
            }
        }
    }
}