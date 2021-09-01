# Snowflake Grant Report
Snowflake Role-based Access Control (RBAC) offers customers powerful tools to configure authorization to secure their systems, including ability to build a hierarchy of roles and assign mix of granular permissions for combined effective permissions. For more information, see [Overview of Access Control](https://docs.snowflake.com/en/user-guide/security-access-control-overview.html).

Snowflake Grant Report extracts Roles and Grants data from Snowflake and provides tabular and visual reports on the Role hierarchy and Grant assignments. The tool also provides ability to compare privilege configuration between two different reports, enabling analysis of privilege changes over time in same or even different accounts.

## Visualizing Role Hierarchy
Visual representation of Role hierarchy and databases used by those Roles, with Roles color-coded to their type and location within the hierarchy, offering online graph visualization as well PNG, SVG and PDF versions.
![](/docs/Hierarchy/ExampleRoleHierarchy.png?raw=true)

For more information, see [Role Hiearchy Reports](../../wiki/Role-Hierarchy-Reports).

## Tabular Report for Grants
All Grants for the TABLE Object Type:
![](docs/Grants/Grants.Tbl.TABLE.png?raw=true)

All Grants granted to specific Roles, filtered by Object Type, showing distinct Privileges on those Objects
![](docs/Grants/Grants.Type.Privilege.png?raw=true)

All Roles created over years and months by different Owner Roles:
![](docs/Roles/Roles.CreatedTimeline.png?raw=true)

Showing all Grants for Schema, Table and View object in a Database:
![](docs/Grants/DB.EXAMPLE.png?raw=true)

For more information, see [Table Reports](../../wiki/Table-Reports).

## Comparing Two Reports 
Audit changes in privileges between two different reports: 
![](docs/Compare/DifferencesTable.png?raw=true)

And a pivot by type:
![](docs/Compare/DifferencesByTypePivot.png?raw=true)

And a view for all objects and their roles:
![](docs/Compare/DifferencesForObjectsAndRoles.png?raw=true)


# Install Prerequisites
## Install SnowSQL
To access data in Snowflake, you need to install SnowSQL on your system as described in [Installing SnowSQL](https://docs.snowflake.com/en/user-guide/snowsql-install-config.html).

Configure named connection parameter as described in [Using Named Connections](https://docs.snowflake.com/en/user-guide/snowsql-start.html#using-named-connections). All authentication option are supported.

Ensure `snowsql` is in the PATH of your shell. Test by running this in your shell:
```
snowsql -v
```
You should see output similar to this:
```
Version: 1.2.12
```

## Install GraphViz
To produce Role and Database Object relationship visualization files locally (SVG, PNG, PDF) that you need to install GraphViz tools from https://graphviz.org/download. 

OS | Notes
--|--
Windows| Choose cmake 64 bit build and during installation, check the option for adding the tools to the PATH
OSX | Choose Homebrew option https://formulae.brew.sh/formula/graphviz
Linux | Choose the package for your distribution

Ensure GraphViz tools are in the path of your shell. You can test by running `dot` executable in your shell:
```
dot -V
```
You should see something like that:
```
dot - graphviz version 2.46.0 (20210118.1747)
```

# Install Application
Snowflake Grant Report can run on Windows, Mac or most Linux distributions. 

Files are in [Releases](https://github.com/Snowflake-Labs/sfgrantreport/releases/latest).

## Install on OSX
Download [Releases](https://github.com/Snowflake-Labs/sfgrantreport/releases/latest)\ `SFGrantReport.osx.<version>.zip` but do not extract the archive yet.

Open terminal/shell of your choice and change working directory to where you saved the file to. 

Run this command in the shell to remove the quarantine attribute that will otherwise stop the application from running:
```
xattr -d com.apple.quarantine SFGrantReport.*.zip
```

Now extract the archive.

## Install on Windows
Download [Releases](https://github.com/Snowflake-Labs/sfgrantreport/releases/latest)\ `SFGrantReport.win.<version>.zip`, save and extract the archive.

## Install on Linux
Download [Releases](https://github.com/Snowflake-Labs/sfgrantreport/releases/latest)\ `SFGrantReport.linux.<version>.zip`, save and extract the archive.

# Run Application
SFGrantReport supports two mutually exclusive ways of getting Role and Grant information:
* Direct: connect to Snowflake directly and run commands using snowsql (`-c, --connection` option)
* Offline: use data extracted from Snowflake ACCOUNT_USAGE views (`-i, --input-folder` option)

## Available Command Line Parameters
Get list of Snowflake Grant Report parameters by running this command in your shell:

OSX or Linux:
```
./SFGrantReport --help
```

Windows:
```
.\SFGrantReport.exe --help
```

You should see something like that:
```
Snowflake Grant Report Version 2021.8.10.0
SFGrantReport 2021.8.10.0
Copyright c 2020-2021

ERROR(S):
  Required option 'c, connection' is missing.
  Required option 'i, input-folder' is missing.
  Required option 'l, left-folder-compare' is missing.
  Required option 'r, right-folder-compare' is missing.

  -c, --connection                       Required. Name of the SnowSQL connection entry that will be used to connect to Snowflake.

  -i, --input-folder                     Required. Folder where the files from ACCOUNT_USAGE share in SNOWFLAKE database are stored.

  -o, --output-folder                    Output folder where to create report.

  -l, --left-folder-compare              Required. Left folder containing report files to compare against.

  -r, --right-folder-compare             Required. Right folder containing report files to compare with.

  -d, --delete-previous-report-output    If true, delete any results of previous processing.

  -s, --sequential-processing            If true, process certain items during extraction and conversion sequentially.

  --help                                 Display this help screen.

  --version                              Display version information.
```

## -c, --connection
SFGrantReport can connect to Snowflake directly to retrieve Role and Grant information. 

Use `-c, --connection` parameter to specify the name of the connection in snowsql configuration file. 

For example, `mysnowflakeaccount` is a named connection configured in the following fashion:

```
[connections.mysnowflakeaccount]
accountname = mysnowflakeaccount
username = myusername
password = ************
warehousename = MY_WAREHOUSE
dbname = MY_DATABASE
```

For example:
```
./SFGrantReport -c mysnowflakeaccount -o ~/Documents/MyAwesomeReport
```
or
```
./SFGrantReport --connection mysnowflakeaccount --output-folder ~/Documents/MyAwesomeReport
```

For full results, the user should have SECURITYADMIN role to to Roles and Users. If user has is a SYSADMIN or below, DESCRIBE USER command is unlikely to return all the data, but grant hierarchy should work.

## -i, --input-folder
It is also possible to run SFGrantReport in offline mode, without connecting to Snowflake directly. 

Use `-i, --input-folder` parameter to specify the path to the folder containing exports from [SNOWFLAKE.ACCOUNT_USAGE](https://docs.snowflake.com/en/sql-reference/account-usage.html) share, and specifically from [GRANTS_TO_ROLES](https://docs.snowflake.com/en/sql-reference/account-usage/grants_to_roles.html) and [GRANTS_TO_USERS](https://docs.snowflake.com/en/sql-reference/account-usage/grants_to_users.html) views. 

The `SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_ROLES` query should typically ran as ACCOUNTADMIN and the output MUST be saved as `GRANTS_TO_ROLES.csv`: 
```
snowsql -c [your named connection name] -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_ROLES" -o output_format=csv -o header=true -o timing=false -o friendly=false > [path to your output]/GRANTS_TO_ROLES.csv
```

The `SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_USERS` query should typically ran as ACCOUNTADMIN and the output MUST be saved as `GRANTS_TO_USERS.csv`:
```
snowsql -c [your named connection name] -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_USERS" -o output_format=csv -o header=true -o timing=false -o friendly=false > [path to your output]/GRANTS_TO_USERS.csv
```

In this example, SnowSQL 'mysnowflakeaccount' named connection is used to connect as ACCOUNTADMIN and output necessary files to `account_usage/GRANTS_TO_ROLES.csv` and `account_usage/GRANTS_TO_USERS.csv`:
```
mkdir account_usage

snowsql -c mysnowflakeaccount -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_ROLES" -o output_format=csv -o header=true -o timing=false -o friendly=false > "account_usage/GRANTS_TO_ROLES.csv"

snowsql -c mysnowflakeaccount -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_USERS" -o output_format=csv -o header=true -o timing=false -o friendly=false > "account_usage/GRANTS_TO_USERS.csv"
```

With those files exported saved, you can invoke the command to process them offline:
```
./SFGrantReport -i path/to/account_usage -o ~/Documents/MyAwesomeOfflineReport
```
or
```
./SFGrantReport --input-folder path/to/account_usage --output-folder ~/Documents/MyAwesomeOfflineReport
```

## -o, --output-folder
Use `-o, --output-folder` parameter to specify where the report files should go (unless you want them created in the same directory you started the tool).

For example, this command uses named connection `mysnowflakeaccount` and creates report in the folder named `MyAwesomeReport` in the Documents folder:
```
./SFGrantReport -c mysnowflakeaccount -o ~/Documents/MyAwesomeReport
```
or
```
./SFGrantReport --connection mysnowflakeaccount --output-folder ~/Documents/MyAwesomeReport
```

Relative paths are supported, like here to go from current folder up two levels:
```
./SFGrantReport --connection mysnowflakeaccount --output-folder ../../MyAwesomeReport
```

## -l, --left-folder-compare
When you have two outputs of same account at two different points at time, or even two different accounts, you can compare them.

Use `-l, --left-folder-compare` parameter to specify where the files are for the left/reference side of the comparison.

## -r, --right-folder-compare
Use `-r, --right-folder-compare` parameter to specify where the files are for the right/difference side of the comparison.

For example, this command uses :
```
./SFGrantReport.exe -l ~/Documents/myaccount/statusonday1 -r ~/Documents/myaccount/statusonday42 -o ~/Documents/myaccount/day1today42comparison
```
or
```
./SFGrantReport.exe --left-folder-compare ~/Documents/myaccount/statusonday1 --right-folder-compare ~/Documents/myaccount/statusonday42 --output-folder ~/Documents/myaccount/day1today42comparison
```

## -d, --delete-previous-report-output
When `-d, --delete-previous-report-output` is specified and the output folder already contains some data, the output folder is cleared.

# Report Results
Results are in various `SFGrantReport.<prefix>.<connectionname>.<timestamp of report generation>.xlsx` documents as well as in the Output folder\RPT (in various CSV/PNG/SVG/PDF files):

```
    Directory: C:\snowflake\GrantReport\Reports\myreport

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d----            2/2/2021  5:08 PM                DATA
d----            2/2/2021  5:08 PM                RPT
-a---            2/2/2021  5:08 PM            224 Snowflake.GrantReport.json
-a---            2/2/2021  5:08 PM           1472 StepDurations.csv
-a---            2/2/2021  5:08 PM          11601 SFGrantReport.ACCOUNT.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM         180197 SFGrantReport.ALL.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          11089 SFGrantReport.DATABASE.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          24313 SFGrantReport.DBGRANTS.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          37701 SFGrantReport.Grants.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9579 SFGrantReport.INTEGRATION.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9301 SFGrantReport.NOTIFICATION_SUBSCRIPTION.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          17246 SFGrantReport.ROLE.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          16730 SFGrantReport.SCHEMA.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM        1333324 SFGrantReport.myreport.202102030106.html
-a---            2/2/2021  5:08 PM           9309 SFGrantReport.STAGE.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          12093 SFGrantReport.TABLE.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          12033 SFGrantReport.USER.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          66038 SFGrantReport.UsersRoles.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM          13331 SFGrantReport.VIEW.myreport.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9816 SFGrantReport.WAREHOUSE.myreport.202102030106.xlsx
```

For more information, see [Documentation](../../wiki/Home).

# Acknowledgements
* Microsoft - Thanks for Visual Studio and .NET Core team for letting us all run CLR code on any platform https://github.com/dotnet/core
* Command Line Parser - Simple and fast https://github.com/commandlineparser/commandline
* CSV File Creation and Parsing - An excellent utility https://github.com/JoshClose/CsvHelper
* JSON Parsing - NewtonSoft JSON is awesome https://www.newtonsoft.com/json
* Logging - NLog is also awesome http://nlog-project.org/ 
* Excel Report Creation - Jan Kallman's excellent helper class is a lifesaver https://github.com/EPPlusSoftware/EPPlus
* GraphViz - Incredibly powerful cross platform visualization https://graphviz.org