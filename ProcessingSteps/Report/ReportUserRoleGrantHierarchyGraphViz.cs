using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

                    // Create a graphviz file for each role
                    foreach (Role role in rolesList)
                    {
                        loggerConsole.Info("Processing visualization for {0}", role);

                        List<RoleHierarchy> thisRoleAndItsRelationsHierarchiesList = FileIOHelper.ReadListFromCSVFile<RoleHierarchy>(FilePathMap.Report_RoleHierarchy_RoleAndItsRelations_FilePath(role.Name), new RoleHierarchyMap());;
                        List<Role> thisRoleAndItsRelationsList = FileIOHelper.ReadListFromCSVFile<Role>(FilePathMap.Report_RoleDetail_RoleAndItsRelations_FilePath(role.Name), new RoleMap());

                        if (thisRoleAndItsRelationsList != null && thisRoleAndItsRelationsHierarchiesList != null)
                        {
                            Dictionary<string, Role> rolesDict = thisRoleAndItsRelationsList.ToDictionary(r => r.Name, k => k);
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
                            foreach (RoleHierarchy roleHierarchy in thisRoleAndItsRelationsHierarchiesList)
                            {
                                if (roleNamesOutput.ContainsKey(roleHierarchy.GrantedTo) == false)
                                {
                                    // Name of the role with color
                                    rolesDict.TryGetValue(roleHierarchy.GrantedTo, out roleBeingOutput);
                                    sbGraphViz.AppendFormat("  \"{0}\"{1};", roleHierarchy.GrantedTo, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                                    roleNamesOutput.Add(roleHierarchy.GrantedTo, roleBeingOutput);
                                }
                                
                                if (roleNamesOutput.ContainsKey(roleHierarchy.Name) == false)
                                {
                                    // Name of the role with color
                                    rolesDict.TryGetValue(roleHierarchy.Name, out roleBeingOutput);
                                    sbGraphViz.AppendFormat("  \"{0}\"{1};", roleHierarchy.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
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
                                // Role to role connector
                                sbGraphViz.AppendFormat(" \"{0}\"->\"{1}\";", roleHierarchy.GrantedTo, roleHierarchy.Name); sbGraphViz.AppendLine();
                            }
                            sbGraphViz.AppendLine(" // /Role hierarchy");

                            #endregion

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
                                foreach (Schema schema in database.Schemas)
                                {
                                    sbGraphViz.AppendFormat("     <tr><td align=\"left\">{0}</td><td align=\"right\">{1}</td><td align=\"right\">{2}</td></tr>", schema.ShortName, schema.Tables.Count, schema.Views.Count); sbGraphViz.AppendLine();
                                }
                                sbGraphViz.AppendLine  ("    </table>>];");

                                // Connect database to schemas
                                sbGraphViz.AppendFormat("   \"{0}\"->\"{0}.schema\" [style=\"invis\"];", database.FullName); sbGraphViz.AppendLine();

                                sbGraphViz.AppendFormat("  }} // /Database {0}", database.FullName); sbGraphViz.AppendLine();

                                databaseIndex++;

                                #region Ouput of individual schema and table details
                                // Commented out
                                // Output schemas
                                // int schemaIndex = 0;
                                // foreach (Schema schema in database.Schemas)
                                // {
                                //     sbGraphViz.AppendFormat("   // Schema {0}", schema.FullName); sbGraphViz.AppendLine();
                                //     sbGraphViz.AppendFormat("   subgraph cluster_db_{0}_schema_{1} {{", databaseIndex, schemaIndex); sbGraphViz.AppendLine();
                                //     sbGraphViz.AppendLine  ("    style=\"filled\";");
                                //     sbGraphViz.AppendLine  ("    fillcolor=\"cornsilk\";");
                                //     sbGraphViz.AppendFormat("    label = \"schema: {0}\";", schema.ShortName); sbGraphViz.AppendLine();
                                //     sbGraphViz.AppendLine  ("    node [shape=\"folder\" fillcolor=\"moccasin\"];");
                                //     sbGraphViz.AppendLine();

                                //     sbGraphViz.AppendFormat("    \"{0}\" [label=\"{1}\nTables: {2}\nViews: {3}\"];", schema.FullName, schema.ShortName, schema.Tables.Count, schema.Views.Count); sbGraphViz.AppendLine();
                                //     sbGraphViz.AppendLine();

                                    // Output Tables
                                    // if (schema.Tables.Count > 0)
                                    // {
                                    //     sbGraphViz.AppendLine  ("    // Tables ");
                                    //     sbGraphViz.AppendFormat("    subgraph cluster_db_{0}_schema_{1}_tables {{", databaseIndex, schemaIndex); sbGraphViz.AppendLine();
                                    //     sbGraphViz.AppendLine  ("     style=\"filled\";");
                                    //     sbGraphViz.AppendLine  ("     fillcolor=\"gold\";");
                                    //     sbGraphViz.AppendLine  ("     label = \"tables\";");
                                    //     sbGraphViz.AppendLine  ("     node [shape=\"rectangle\" fillcolor=\"darkseagreen\"];");
                                    //     sbGraphViz.AppendLine();

                                    //     sbGraphViz.AppendFormat("     \"{0}_tables\" [label=\"{1}\"];", schema.FullName, String.Join("\\n", schema.Tables.Select(t => t.ShortName))); sbGraphViz.AppendLine();

                                    //     // int tableIndex = 0;
                                    //     // foreach (Table table in schema.Tables)
                                    //     // {
                                    //     //     sbGraphViz.AppendFormat("     \"{0}\" [label=\"{1}\"];", table.FullName, table.ShortName); sbGraphViz.AppendLine();
                                            
                                    //     //     tableIndex++;
                                    //     // }

                                    //     // // Connect tables
                                    //     // if (schema.Tables.Count > 1)
                                    //     // {
                                    //     //     sbGraphViz.AppendLine  ("     // Connect Tables");
                                    //     //     sbGraphViz.AppendFormat("     {0} [style=\"invis\"];", String.Join("->", schema.Tables.Select(s => String.Format("\"{0}\"", s.FullName)))); sbGraphViz.AppendLine();
                                    //     // }

                                    //     sbGraphViz.AppendLine  ("    }// /Tables ");
                                    //     sbGraphViz.AppendLine();

                                    // }
                                    
                                    // if (schema.Views.Count > 0)
                                    // {
                                    //     // Output Views
                                    //     sbGraphViz.AppendLine  ("    // Views ");
                                    //     sbGraphViz.AppendFormat("    subgraph cluster_db_{0}_schema_{1}_Views {{", databaseIndex, schemaIndex); sbGraphViz.AppendLine();
                                    //     sbGraphViz.AppendLine  ("     style=\"filled\";");
                                    //     sbGraphViz.AppendLine  ("     fillcolor=\"bisque\";");
                                    //     sbGraphViz.AppendLine  ("     label = \"views\";");
                                    //     sbGraphViz.AppendLine  ("     node [shape=\"rectangle\" fillcolor=\"darkseagreen\"];");
                                    //     sbGraphViz.AppendLine();

                                    //     sbGraphViz.AppendFormat("     \"{0}_views\" [label=\"{1}\"];", schema.FullName, String.Join("\\n", schema.Views.Select(v => v.ShortName))); sbGraphViz.AppendLine();

                                    //     // int viewIndex = 0;
                                    //     // foreach (View view in schema.Views)
                                    //     // {
                                    //     //     sbGraphViz.AppendFormat("     \"{0}\" [label=\"{1}\"];", view.FullName, view.ShortName); sbGraphViz.AppendLine();
                                            
                                    //     //     viewIndex++;
                                    //     // }

                                    //     // // Connect views
                                    //     // if (schema.Views.Count > 1)
                                    //     // {
                                    //     //     sbGraphViz.AppendLine  ("     // Connect Views");
                                    //     //     sbGraphViz.AppendFormat("     {0} [style=\"invis\"];", String.Join("->", schema.Views.Select(s => String.Format("\"{0}\"", s.FullName)))); sbGraphViz.AppendLine();
                                    //     // }

                                    //     sbGraphViz.AppendLine  ("    }// /Views ");
                                    //     sbGraphViz.AppendLine();

                                    // }

                                //     sbGraphViz.AppendFormat("   }}// /Schema {0}", schema.FullName); sbGraphViz.AppendLine();
                                //     sbGraphViz.AppendLine();
                                    
                                //     schemaIndex++;
                                // }

                                // Connect schemas
                                // sbGraphViz.AppendLine("  // Connect Schemas");
                                // foreach (Schema schema in database.Schemas)
                                // {
                                //     sbGraphViz.AppendFormat("  \"{0}\"->\"{1}\" [style=\"invis\"];", database.FullName, schema.FullName); sbGraphViz.AppendLine();
                                // }
                                // if (database.Schemas.Count > 0)
                                // {
                                //     sbGraphViz.AppendLine("  // Connect Schemas");
                                //     sbGraphViz.AppendFormat("   \"{0}\"->{1} [style=\"invis\"];", database.FullName, String.Join("->", database.Schemas.Select(s => String.Format("\"{0}\"", s.FullName)))); sbGraphViz.AppendLine();
                                // }
                                #endregion
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
                                            sbGraphViz.AppendFormat(" \"{0}\"->\"{1}\" [color=\"darkkhaki\"];", grant.GrantedTo, grant.ObjectName); sbGraphViz.AppendLine();
                                        }
                                    }
                                }
                            }

                            sbGraphViz.AppendLine(" // /Roles using databases");

                            #endregion

                            #region Legend

                            // Output Legend
                            sbGraphViz.AppendLine();
                            string legend = @" // Legend
  ""legend"" [label=<
  <table border=""0"" cellborder=""0"" bgcolor=""white"">
  <tr><td align=""center"">Legend</td></tr>
  <tr><td align=""left"" bgcolor=""lightgray"">BUILT IN</td></tr>
  <tr><td align=""left"" bgcolor=""beige"">SCIM</td></tr>
  <tr><td align=""left"" bgcolor=""wheat"">ROLE MANAGEMENT</td></tr>
  <tr><td align=""left"" bgcolor=""orchid"">FUNCTIONAL</td></tr>
  <tr><td align=""left"" bgcolor=""plum"">FUNCTIONAL NOT UNDER SYSADMIN</td></tr>
  <tr><td align=""left"" bgcolor=""lightblue"">ACCESS</td></tr>
  <tr><td align=""left"" bgcolor=""azure"">ACCESS NOT UNDER SYSADMIN</td></tr>
  <tr><td align=""left"" bgcolor=""orange"">NOT UNDER ACCOUNTADMIN</td></tr>
  </table>>];";
                            sbGraphViz.AppendLine(legend);
                            
                            // sbGraphViz.AppendLine();
                            // sbGraphViz.AppendLine(" // Legend");
                            // sbGraphViz.AppendLine(" subgraph cluster_legend {");
                            // sbGraphViz.AppendLine("  style=\"filled\";");
                            // sbGraphViz.AppendLine("  fillcolor=\"lightgrey\";");
                            // sbGraphViz.AppendLine("  label = \"Legend\";");
                            // sbGraphViz.AppendLine("  edge [style=\"invis\"];");

                            // roleBeingOutput = new Role {Name = "BUILT IN", Type = RoleType.BuiltIn};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "SCIM", Type = RoleType.SCIM};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "ROLE MANAGEMENT", Type = RoleType.RoleManagement};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "FUNCTIONAL", Type = RoleType.Functional};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "FUNCTIONAL NOT UNDER SYSADMIN", Type = RoleType.FunctionalNotUnderSysadmin};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "ACCESS", Type = RoleType.Access};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "ACCESS NOT UNDER SYSADMIN", Type = RoleType.AccessNotUnderSysadmin};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "NOT UNDER ACCOUNTADMIN", Type = RoleType.NotUnderAccountAdmin};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // roleBeingOutput = new Role {Name = "UNKNOWN", Type = RoleType.Unknown};
                            // sbGraphViz.AppendFormat("  \"{0}\"{1};", roleBeingOutput.Name, getRoleStyleAttribute(roleBeingOutput)); sbGraphViz.AppendLine();
                            // sbGraphViz.AppendLine("  \"BUILT IN\"->\"SCIM\"-> \"ROLE MANAGEMENT\" -> \"FUNCTIONAL\" -> \"FUNCTIONAL NOT UNDER SYSADMIN\" -> \"ACCESS\" -> \"ACCESS NOT UNDER SYSADMIN\" ->\"NOT UNDER ACCOUNTADMIN\"->\"UNKNOWN\"");
                            // sbGraphViz.AppendLine(" }");
                            // sbGraphViz.AppendLine(" // /Legend");

                            #endregion

                            // Close the graph
                            sbGraphViz.AppendLine("}");
    
                            FileIOHelper.SaveFileToPath(sbGraphViz.ToString(), FilePathMap.Report_GraphViz_RoleAndItsRelationsGrants_FilePath(role.Name), false);
                        }
                    }

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
                            xmlWriter.WriteString("Opt1");
                            xmlWriter.WriteEndElement(); // </a>

                            // http://magjac.com/graphviz-visual-editor
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", String.Format("http://magjac.com/graphviz-visual-editor/?dot={0}", Uri.EscapeDataString(graphText)));
                            xmlWriter.WriteString("Opt2");
                            xmlWriter.WriteEndElement(); // </a>

                            // https://stamm-wilbrandt.de/GraphvizFiddle/2.1.2/index.html
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", String.Format("https://stamm-wilbrandt.de/GraphvizFiddle/2.1.2/index.html?#{0}", Uri.EscapeDataString(graphText)));
                            xmlWriter.WriteString("Opt3");
                            xmlWriter.WriteEndElement(); // </a>

                            // https://dreampuf.github.io/GraphvizOnline
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", String.Format("https://dreampuf.github.io/GraphvizOnline/#{0}", Uri.EscapeDataString(graphText)));
                            xmlWriter.WriteString("Opt4");
                            xmlWriter.WriteEndElement(); // </a>

                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_SVG_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("SVG");
                            // xmlWriter.WriteStartElement("img");
                            // xmlWriter.WriteAttributeString("src", "https://upload.wikimedia.org/wikipedia/commons/4/4f/SVG_Logo.svg"); 
                            // xmlWriter.WriteAttributeString("width", "24");
                            // xmlWriter.WriteAttributeString("height", "24");
                            // xmlWriter.WriteEndElement(); // </img>
                            xmlWriter.WriteEndElement(); // </a>
                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_PNG_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("PNG");
                            // xmlWriter.WriteStartElement("img");
                            // xmlWriter.WriteAttributeString("src", "https://icons.iconarchive.com/icons/untergunter/leaf-mimes/24/png-icon.png"); 
                            // xmlWriter.WriteAttributeString("width", "24");
                            // xmlWriter.WriteAttributeString("height", "24");
                            // xmlWriter.WriteEndElement(); // </img>
                            xmlWriter.WriteEndElement(); // </a>
                            xmlWriter.WriteEndElement(); // </td>

                            xmlWriter.WriteStartElement("td"); 
                            xmlWriter.WriteStartElement("a"); 
                            xmlWriter.WriteAttributeString("href", FilePathMap.Report_Diagram_PDF_RoleAndItsRelationsGrants_FilePath(role.Name, false));
                            xmlWriter.WriteString("PDF");
                            // xmlWriter.WriteStartElement("img");
                            // xmlWriter.WriteAttributeString("src", "https://www.adobe.com/content/dam/cc/en/legal/images/badges/PDF_24.png"); 
                            // xmlWriter.WriteAttributeString("width", "24");
                            // xmlWriter.WriteAttributeString("height", "24");
                            // xmlWriter.WriteEndElement(); // </img>
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
                        foreach (Role role in rolesList)
                        {
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
                        }
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

        private static string getRoleStyleAttribute(Role role)
        {
            string styleAttribute = String.Empty;;
            if (role == null) return styleAttribute ;
            
            // Color names being used here https://graphviz.org/doc/info/colors.html
            switch (role.Type)
            {
                case RoleType.Unknown:
                    break;
                case RoleType.BuiltIn:
                    styleAttribute = " [fillcolor=\"lightgray\"]";
                    break;
                case RoleType.SCIM:
                    styleAttribute = " [fillcolor=\"beige\"]";
                    break;
                case RoleType.RoleManagement:
                    styleAttribute = " [fillcolor=\"wheat\"]";
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
                case RoleType.NotUnderAccountAdmin:
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