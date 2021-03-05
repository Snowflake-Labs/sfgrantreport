using NLog;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Snowflake.GrantReport.ReportObjects;

namespace Snowflake.GrantReport.ProcessingSteps
{

    public class JobStepBase
    {
        #region Constants for Common Reports sheets

        internal const string SHEET_PARAMETERS = "Parameters";
        internal const string SHEET_TOC = "Contents";

        internal const string TABLE_PARAMETERS_TARGETS = "t_InputTargets";
        internal const string TABLE_TOC = "t_TOC";

        #endregion

        #region Constants for various report colors

        // Hyperlink colors
        internal static Color colorBlueForHyperlinks = Color.Blue;

        #endregion

        internal static Logger logger = LogManager.GetCurrentClassLogger();
        internal static Logger loggerConsole = LogManager.GetLogger("Snowflake.GrantReport.Console");

        public FilePathMap FilePathMap { get; set; }

        public void DisplayJobStepStartingStatus(ProgramOptions programOptions)
        {
            logger.Info("{0}({0:d}): Starting", programOptions.ReportJob.Status);
            loggerConsole.Trace("{0}({0:d}): Starting", programOptions.ReportJob.Status);
        }

        public void DisplayJobStepEndedStatus(ProgramOptions programOptions, Stopwatch stopWatch)
        {
            logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", programOptions.ReportJob.Status, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", programOptions.ReportJob.Status, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }

        public virtual bool Execute(ProgramOptions programOptions)
        {
            return false;
        }

        public virtual bool ShouldExecute(ProgramOptions programOptions)
        {
            return false;
        }

        #region Functions for Snowflake object names 

        public static string quoteObjectIdentifier(string objectName)
        {
            // Per https://docs.snowflake.com/en/sql-reference/identifiers-syntax.html must quote things if they contain special characters
            char[] roleNameSpecialChars = { 
                ' ', 
                '!', 
                '"', 
                '#', 
                '$', 
                '%', 
                '&', 
                '\'', 
                '(', 
                ')', 
                '^', 
                '.', 
                ',', 
                '*', 
                '-', 
                '=', 
                '/', 
                '\\', 
                '[', 
                ']', 
                ':', 
                '?', 
                '|', 
                '"', 
                '<', 
                '>', 
                '{', 
                '}', 
                '~', 
                '`' };
            foreach (var c in roleNameSpecialChars)
            {
                // Escape the embedded "
                if (c == '"')
                {
                    objectName = objectName.Replace(c.ToString(), "\"\"");
                }
                // If it is quotable, wrap it in "
                if (objectName.Contains(c, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    return String.Format("\"{0}\"", objectName);
                }
            }

            // If we made it here, let's check for the first letter to be A-Z or a-z or underscore
            byte[] firstCharByte = Encoding.UTF8.GetBytes(objectName.Substring(0, 1));
            if ((firstCharByte[0] >= 0x41 && firstCharByte[0] <= 0x5A) ||    // 0x41=A 0x5A=Z
                (firstCharByte[0] >= 0x61 && firstCharByte[0] <= 0x7A) ||    // 0x61=a 0x7A=z
                firstCharByte[0] == 0x5F                                     // 0x5F=_
               )
            {
                // Valid identifier
            }
            else
            {
                return String.Format("\"{0}\"", objectName);
            }
            return objectName;
        }

        #endregion

        #region Hierarchy of objects and their grants

        public Account buildObjectHierarchyWithGrants()
        {
            loggerConsole.Info("Loading grants");

            // Load selected types of grants to visualize
            List<Grant> grantsToAccountList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("ACCOUNT"), new GrantMap());
            if (grantsToAccountList != null) loggerConsole.Trace("Grants in Accounts {0}", grantsToAccountList.Count);

            List<Grant> grantsToDatabaseList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("DATABASE"), new GrantMap());
            if (grantsToDatabaseList != null) loggerConsole.Trace("Grants in Databases {0}", grantsToDatabaseList.Count);

            List<Grant> grantsToSchemaList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("SCHEMA"), new GrantMap());
            if (grantsToSchemaList != null) loggerConsole.Trace("Grants in Schemas {0}", grantsToSchemaList.Count);

            List<Grant> grantsToTableList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("TABLE"), new GrantMap());
            if (grantsToTableList != null) loggerConsole.Trace("Grants in Tables {0}", grantsToTableList.Count);

            List<Grant> grantsToViewList = FileIOHelper.ReadListFromCSVFile<Grant>(FilePathMap.Report_RoleGrant_ObjectType_FilePath("VIEW"), new GrantMap());
            if (grantsToViewList!= null) loggerConsole.Trace("Grants in Views {0}", grantsToViewList.Count);

            Account account = new Account();
            account.FullName = "TODO";
            account.ShortName = account.FullName;

            // Create object hiearchy for the grants
            if (grantsToAccountList != null)
            {
                var grantsToAccountGroups = grantsToAccountList.GroupBy(g => g.ObjectName);
                foreach (var grantsToAccountGroup in grantsToAccountGroups)
                {
                    account.FullName = grantsToAccountGroup.Key;
                    account.ShortName = account.FullName;
                    account.Grants = grantsToAccountGroup .ToList();

                    // Only do one account, the first one. Should probably be the only one
                    break;
                }
            }

            if (grantsToDatabaseList != null)
            {
                int j = 0;

                loggerConsole.Info("Loading databases from {0} grants", grantsToDatabaseList.Count);

                var grantsToDatabaseGroups = grantsToDatabaseList.GroupBy(g => g.ObjectName);
                foreach (var grantsToDatabaseGroup in grantsToDatabaseGroups)
                {
                    Grant firstGrantInGroup = grantsToDatabaseGroup.First();

                    Database database = new Database();
                    database.Account = account;
                    database.FullName = firstGrantInGroup.ObjectNameUnquoted;
                    database.ShortName = firstGrantInGroup.EntityName;
                    database.Grants = grantsToDatabaseGroup.ToList();

                    account.Databases.Add(database);

                    j++;
                    if (j % 100 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }
                Console.WriteLine();
                loggerConsole.Info("Done {0} Databases", j);
            }

            account.DatabasesDict = account.Databases.ToDictionary(k => k.ShortName, d => d, StringComparer.InvariantCulture);

            if (grantsToSchemaList != null)
            {
                int j = 0;

                loggerConsole.Info("Loading schemas from {0} grants", grantsToSchemaList.Count);

                var grantsToSchemaGroups = grantsToSchemaList.GroupBy(g => g.ObjectName);
                foreach (var grantsToSchemaGroup in grantsToSchemaGroups)
                {   
                    Grant firstGrantInGroup = grantsToSchemaGroup.First();

                    Schema schema = new Schema();
                    schema.FullName = firstGrantInGroup.ObjectNameUnquoted;
                    schema.ShortName = firstGrantInGroup.EntityName;
                    if (account.DatabasesDict.ContainsKey(firstGrantInGroup.DBName) == true)
                    {
                        schema.Database = account.DatabasesDict[firstGrantInGroup.DBName];
                    }
                    else
                    {
                        Database database = new Database();
                        database.Account = account;
                        database.FullName = firstGrantInGroup.DBName;
                        database.ShortName = firstGrantInGroup.DBName;

                        account.Databases.Add(database);
                        schema.Database = database;

                        account.DatabasesDict.Add(database.ShortName, database);
                    }
                    schema.Grants = grantsToSchemaGroup.ToList();

                    schema.Database.Schemas.Add(schema);

                    j++;
                    if (j % 250 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }
                Console.WriteLine();
                loggerConsole.Info("Done {0} Schemas", j);
            }

            foreach (Database database in account.Databases)
            {
                database.SchemasDict = database.Schemas.ToDictionary(k => k.ShortName, s => s, StringComparer.InvariantCulture);
            }

            if (grantsToTableList != null)
            {
                int j = 0;

                loggerConsole.Info("Loading tables from {0} grants", grantsToTableList.Count);

                var grantsToTableGroups = grantsToTableList.GroupBy(g => g.ObjectName);
                foreach (var grantsToTableGroup in grantsToTableGroups)
                {
                    Grant firstGrantInGroup = grantsToTableGroup.First();

                    Table table = new Table();
                    table.FullName = firstGrantInGroup.ObjectNameUnquoted;
                    table.ShortName = firstGrantInGroup.EntityName;
                    if (account.DatabasesDict.ContainsKey(firstGrantInGroup.DBName) == true)
                    {
                        Database thisDatabase = account.DatabasesDict[firstGrantInGroup.DBName];
                        if (thisDatabase.SchemasDict.ContainsKey(firstGrantInGroup.SchemaName) == true)
                        {
                            table.Schema = thisDatabase.SchemasDict[firstGrantInGroup.SchemaName];
                        }
                    }
                    //table.Schema = account.Databases.Where(d => d.ShortName == firstGrantInGroup.DBName).FirstOrDefault().Schemas.Where(s => s.ShortName == firstGrantInGroup.SchemaName).FirstOrDefault();
                    table.Grants = grantsToTableGroup.ToList();

                    if (table.Schema != null)
                    {
                        table.Schema.Tables.Add(table);
                    }

                    j++;
                    if (j % 5000 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }
                Console.WriteLine();
                loggerConsole.Info("Done {0} Tables", j);
            }

            if (grantsToViewList != null)
            {
                int j = 0;

                loggerConsole.Info("Loading views from {0} grants", grantsToViewList.Count);

                var grantsToViewGroups = grantsToViewList.GroupBy(g => g.ObjectName);
                foreach (var grantsToViewGroup in grantsToViewGroups)
                {
                    Grant firstGrantInGroup = grantsToViewGroup.First();

                    View view = new View();
                    view.FullName = firstGrantInGroup.ObjectNameUnquoted;
                    view.ShortName = firstGrantInGroup.EntityName;
                    if (account.DatabasesDict.ContainsKey(firstGrantInGroup.DBName) == true)
                    {
                        Database thisDatabase = account.DatabasesDict[firstGrantInGroup.DBName];
                        if (thisDatabase.SchemasDict.ContainsKey(firstGrantInGroup.SchemaName) == true)
                        {
                            view.Schema = thisDatabase.SchemasDict[firstGrantInGroup.SchemaName];
                        }
                    }
                    //view.Schema = account.Databases.Where(d => d.ShortName == firstGrantInGroup.DBName).FirstOrDefault().Schemas.Where(s => s.ShortName == firstGrantInGroup.SchemaName).FirstOrDefault();
                    view.Grants = grantsToViewGroup.ToList();

                    if (view.Schema != null)
                    {
                        view.Schema.Views.Add(view);
                    }
                    

                    j++;
                    if (j % 5000 == 0)
                    {
                        Console.Write("{0}.", j);
                    }
                }
                Console.WriteLine();
                loggerConsole.Info("Done {0} Views", j);
            }

            return account;
        }

        #endregion

        #region Helper function to render sheets

        internal static void fillReportParametersSheet(ExcelWorksheet sheet, ProgramOptions programOptions, string reportName)
        {

            int l = 1;
            sheet.Cells[l, 1].Value = "Table of Contents";
            sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[l, 2].StyleName = "HyperLinkStyle";
            l++; l++;
            sheet.Cells[l, 1].Value = reportName;
            l++; l++;
            sheet.Cells[l, 1].Value = "Version";
            sheet.Cells[l, 2].Value = programOptions.ReportJob.Version;
            l++; 
            sheet.Cells[l, 1].Value = "Retrieved On Local";
            sheet.Cells[l, 2].Value = programOptions.ReportJob.DataRetrievedOn.ToString("G");
            sheet.Cells[l, 3].Value = TimeZoneInfo.Local.DisplayName;
            l++;
            sheet.Cells[l, 1].Value = "Retrieved On UTC";
            sheet.Cells[l, 2].Value = programOptions.ReportJob.DataRetrievedOnUtc.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Connection";
            sheet.Cells[l, 2].Value = programOptions.ReportJob.Connection;
            l++;
            sheet.Cells[l, 1].Value = "Input Folder";
            sheet.Cells[l, 2].Value = programOptions.ReportJob.InputFolder;
            l++;

            sheet.Column(1).Width = 25;
            sheet.Column(2).Width = 25;
            sheet.Column(3).Width = 25;

            return;
        }

        internal static void fillTableOfContentsSheet(ExcelWorksheet sheet, ExcelPackage excelReport)
        {
            sheet.Cells[1, 1].Value = "Sheet Name";
            sheet.Cells[1, 2].Value = "# Entities";
            sheet.Cells[1, 3].Value = "Link";
            int rowNum = 1;
            foreach (ExcelWorksheet sheetInWorkbook in excelReport.Workbook.Worksheets)
            {
                rowNum++;
                sheet.Cells[rowNum, 1].Value = sheetInWorkbook;
                sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", sheetInWorkbook.Name);
                sheet.Cells[rowNum, 3].StyleName = "HyperLinkStyle";
                if (sheetInWorkbook.Tables.Count > 0)
                {
                    sheet.Cells[rowNum, 2].Value = sheetInWorkbook.Tables[0].Address.Rows - 1;
                }
                else if(sheetInWorkbook.PivotTables.Count > 0)
                {
                    sheet.Cells[rowNum, 2].Value = String.Format("{0} rows x {1} columns x {2} filters", sheetInWorkbook.PivotTables[0].RowFields.Count, sheetInWorkbook.PivotTables[0].ColumnFields.Count, sheetInWorkbook.PivotTables[0].PageFields.Count);
                }
            }
            ExcelRangeBase range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
            ExcelTable table = sheet.Tables.Add(range, TABLE_TOC);
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
            table.ShowTotal = false;

            sheet.Column(table.Columns["Sheet Name"].Position + 1).Width = 35;
            sheet.Column(table.Columns["# Entities"].Position + 1).Width = 25;

            return;
        }

        #endregion

        #region Helper function for various entity naming in Excel

        internal static string getShortenedNameForExcelSheet(string sheetName)
        {
            // First, strip out unsafe characters
            sheetName = getExcelTableOrSheetSafeString(sheetName);

            // Second, shorten the string 
            if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

            return sheetName;
        }

        internal static string getExcelTableOrSheetSafeString(string stringToClear)
        {
            char[] excelTableInvalidChars = { ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', ',', '/', '\\', '[', ']', ':', '?', '|', '"', '<', '>' };
            foreach (var c in excelTableInvalidChars)
            {
                stringToClear = stringToClear.Replace(c, '_');
            }

            return stringToClear;
        }

        #endregion

        #region Helper functions to build Pivot tables in Excel

        internal static void setDefaultPivotTableSettings(ExcelPivotTable pivot)
        {
            pivot.ApplyWidthHeightFormats = false;
            pivot.DataOnRows = false;
        }

        internal static void addFilterFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addFilterFieldToPivot(pivot, fieldName, eSortType.None);
        }

        internal static void addFilterFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldF = pivot.PageFields.Add(pivot.Fields[fieldName]);
            fieldF.Sort = sort;
        }
        
        internal static void addFilterFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort, bool isMultiSelect)
        {
            ExcelPivotTableField fieldF = pivot.PageFields.Add(pivot.Fields[fieldName]);
            fieldF.Sort = sort;
            fieldF.MultipleItemSelectionAllowed = isMultiSelect;
        }

        internal static void addRowFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addRowFieldToPivot(pivot, fieldName, eSortType.None);
        }

        internal static void addRowFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields[fieldName]);
            fieldR.Compact = false;
            fieldR.Outline = false;
            fieldR.SubTotalFunctions = eSubTotalFunctions.None;
            fieldR.Sort = sort;
        }

        internal static void addColumnFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addColumnFieldToPivot(pivot, fieldName, eSortType.None);
        }

        internal static void addColumnFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields[fieldName]);
            fieldC.Compact = false;
            fieldC.Outline = false;
            fieldC.SubTotalFunctions = eSubTotalFunctions.None;
            fieldC.Sort = sort;
        }

        internal static void addDataFieldToPivot(ExcelPivotTable pivot, string fieldName, DataFieldFunctions function)
        {
            addDataFieldToPivot(pivot, fieldName, function, String.Empty);
        }

        internal static void addDataFieldToPivot(ExcelPivotTable pivot, string fieldName, DataFieldFunctions function, string displayName)
        {
            ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields[fieldName]);
            fieldD.Function = function;
            if (displayName.Length != 0)
            {
                fieldD.Name = displayName;
            }
        }

        #endregion
    }
}
