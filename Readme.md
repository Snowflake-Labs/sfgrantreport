# Snowflake Grant Report
Snowflake Role-based Access Control (RBAC) offers customers powerful tools to configure authorization to secure their systems, including ability to build a hierarchy of roles and assign mix of granular permissions for combined effective permissions. For more information, see [Overview of Access Control](https://docs.snowflake.com/en/user-guide/security-access-control-overview.html).

Snowflake Grant Report extracts Roles and Grants data from Snowflake and provides tabular and visual reports on the Role hierarchy and Grant assignments.

## Visualizing Role Hierarchy
Visual representation of Role hierarchy and databases used by those Roles, with Roles color-coded to their type and location within the hierarchy, offering online graph visualization as well PNG, SVG and PDF versions.
![](/docs/Hierarchy/ExampleRoleHierarchy.png?raw=true)

For more information, see [Role Hiearchy Reports](../../wiki/Role-Hierarchy-Reports).

## Tabular Report
All Grants for the TABLE Object Type:
![](docs/Grants/Grants.Tbl.TABLE.png?raw=true)

All Grants granted to specific Roles, filtered by Object Type, showing distinct Privileges on those Objects
![](docs/Grants/Grants.Type.Privilege.png?raw=true)

All Roles created over years and months by different Owner Roles:
![](docs/Roles/Roles.CreatedTimeline.png?raw=true)

Showing all Grants for Schema, Table and View object in a Database:

![](docs/Grants/DB.EXAMPLE.png?raw=true)

For more information, see [Table Reports](../../wiki/Table-Reports).

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

## Install on OSX
Download `SFGrantReport.osx.<version>.zip` but do not extract the archive yet.

Open terminal/shell of your choice and change working directory to where you saved the file to. 

Run this command in the shell to remove the quarantine attribute that will otherwise stop the application from running:
```
xattr -d com.apple.quarantine SFGrantReport.*.zip
```

Now extract the archive.

## Install on Windows
Download `SFGrantReport.win.<version>.zip`, save and extract the archive.

## Install on Linux
Download `SFGrantReport.linux.<version>.zip`, save and extract the archive.

# Run Application
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
Snowflake Grant Report Version 2021.2.12.0
SFGrantReport 2021.2.12.0
Copyright c 2020-2021

ERROR(S):
  Required option 'c, connection' is missing.
  Required option 'i, input-folder' is missing.

  -c, --connection                       Required. Name of the SnowSQL connection entry that will be used to connect to Snowflake.

  -i, --input-folder                     Required. Folder where the files from ACCOUNT_USAGE share in SNOWFLAKE database are stored.

  -o, --output-folder                    Output folder where to create report.

  -d, --delete-previous-report-output    If true, delete any results of previous processing.

  --help                                 Display this help screen.

  --version                              Display version information.
```

## -c/--connection
SFGrantReport can connect to Snowflake directly to retrieve Role and Grant information. 

Use `-c/--connection` parameter to specify the name of the connection in snowsql configuration file. 

For example, `mysnowflakeaccount` is a named connection configured in the following fashion:

```
[connections.mysnowflakeaccount]
accountname = mysnowflakeaccount
username = myusername
password = ************
warehousename = MY_WAREHOUSE
dbname = MY_DATABASE
```

For full results, the user must have SECURITYADMIN role to to Roles and Users. If user has is a SYSADMIN or below, DESCRIBE USER command is unlikely to return all the data, but grant hierarchy should work.

## -i, --input-folder
It is also possible to run SFGrantReport in offline mode, without connecting to Snowflake directly. 

Use '-i, --input-folder' parameter to specify the path to the folder containing exports from [SNOWFLAKE.ACCOUNT_USAGE](https://docs.snowflake.com/en/sql-reference/account-usage.html) share, and specifically from [GRANTS_TO_ROLES](https://docs.snowflake.com/en/sql-reference/account-usage/grants_to_roles.html) and [GRANTS_TO_USERS](https://docs.snowflake.com/en/sql-reference/account-usage/grants_to_users.html) views. 

The `SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_ROLES` query output must be ran as ACCOUNTADMIN and must be saved as `GRANTS_TO_ROLES.csv`: 
```
snowsql -c [your named connection name] -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_ROLES;" -o output_format=csv -o header=true -o timing=false -o friendly=false > [path to your output]/GRANTS_TO_ROLES.csv
```

The `SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_USERS` query output MUST be saved as `GRANTS_TO_USERS.csv`:
```
snowsql -c [your named connection name] -r ACCOUNTADMIN -q "SELECT * FROM SNOWFLAKE.ACCOUNT_USAGE.GRANTS_TO_USERS;" -o output_format=csv -o header=true -o timing=false -o friendly=false > [path to your output]/GRANTS_TO_USERS.csv
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
or
./SFGrantReport --input-folder path/to/account_usage --output-folder ~/Documents/MyAwesomeOfflineReport
```

## -o/--output-folder
Use `-o/--output-folder` parameter to specify where the report files should go (unless you want them created in the same directory you started the tool.

For example, this command uses named connection `mysnowflakeaccount` and creates report in the folder named `MyAwesomeReport` in the Documents folder:
```
./SFGrantReport -c mysnowflakeaccount -o ~/Documents/MyAwesomeReport
or
./SFGrantReport --connection mysnowflakeaccount --output-folder ~/Documents/MyAwesomeReport
```

Relative paths are supported, like here to go from current folder up two levels:
```
./SFGrantReport --connection mysnowflakeaccount --output-folder ../../MyAwesomeReport
```

## -d, --delete-previous-report-output
If the output folder already contains some data, when `-d, --delete-previous-report-output` is specified, the output folder is cleared.

# Find Results
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