# Snowflake Grant Report
Snowflake Grant Report extracts Roles and Grants data from Snowflake and provides tabular and visual reports on the Role hierarchy and Grant assignments.

# Install Prerequisites
## SnowSQL
To access data in Snowflake, you need to install SnowSQL on your system as described in https://docs.snowflake.com/en/user-guide/snowsql-install-config.html.

Configure named connection parameter as described in https://docs.snowflake.com/en/user-guide/snowsql-start.html#using-named-connections. Any authentication option you choose is supported.

Ensure SnowSQL is in the PATH of your shell. You can test by running `snowsql` executable in your shell:
```
snowsql -v
```
You should see `snowsql` executable output something similar to this:
```
Version: 1.2.10
```

## GraphViz
To produce role and grant relationship visualization files locally (SVG, PNG, PDF), install GraphViz tools from https://graphviz.org/download. 

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

# Install SnowGrantReport
## OSX
Download `SnowGrantReport.osx.<version>.zip` but do not extract the archive yet.

Open terminal/shell of your choice and change working directory to where you saved the file to. 

Run this command in the shell to remove the quarantine attribute that will otherwise stop the application from running:
```
xattr -d com.apple.quarantine SnowGrantReport.*.zip
```

Now extract the archive.

## Windows
Download `SnowGrantReport.win.<version>.zip`, save and extract the archive.

## Linux
Download `SnowGrantReport.linux.<version>.zip` and extract the archive

Run these commands to mark the file as executable:
```
cd SnowflakeGrantReport.linux.<version>
chmod +x SnowGrantReport
```

# Validate SnowGrantReport is Installed
Validate that SnowGrantReport is working by running this command in the shell:

OSX or Linux:
```
./SnowGrantReport --help
```

Windows:
```
.\SnowGrantReport.exe --help
```

You should see something like that:
```
Snowflake Grant Report Version 2021.2.4.0
Snowflake.GrantReport 2021.2.4.0
Copyright c 2020-2021

  -c, --connection                       Required. Name of the SnowSQL connection entry that will be used to connect to Snowflake.

  -o, --output-folder                    Output folder where to create report.

  -d, --delete-previous-report-output    If true, delete any results of previous processing.

  --help                                 Display this help screen.

  --version                              Display version information.
```

# Run SnowGrantReport
Use `-c/--connection` parameter to specify the name of the connection in snowsql configuration file. 

For example, `mysnowflakeaccount` is a named connection configured in the following fashion:

```
[connections.mysnowflakeaccount]
accountname = mysnowflakeaccount
username = myusername
password = ************
warehousename = MY_WH
dbname = MY_DB
```

The user must possess SECURITYADMIN role. 

Use `-o/--output-folder` parameter to specify where the report files should go (unless you want them created in the same directory you started the tool.

For example, this command uses named connection `mysnowflakeaccount` and creates report in the folder named `MyAwesomeReport` in current documents folder:
```
./SnowGrantReport --connection mysnowflakeaccount --output-folder ~/Documents/MyAwesomeReport
```

Relative paths are supported, like here to go from current folder up two levels:
```
./SnowGrantReport --connection mysnowflakeaccount --output-folder ../../MyAwesomeReport
```

# Find Results
Results are in various `UsersRolesGrants.<prefix><connectionname>.<timestamp of report generation>.xlsx` documents as well as in the Output folder\RPT (in various CSV/PNG/SVG/PDF files):

```
    Directory: C:\snowflake\GrantReport\Reports\myreport

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d----            2/2/2021  5:08 PM                DATA
d----            2/2/2021  5:08 PM                RPT
-a---            2/2/2021  5:08 PM            224 Snowflake.GrantReport.json
-a---            2/2/2021  5:08 PM           1472 StepDurations.csv
-a---            2/2/2021  5:08 PM          11601 UsersRolesGrants.ACCOUNT.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM         180197 UsersRolesGrants.ALL.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          11089 UsersRolesGrants.DATABASE.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          24313 UsersRolesGrants.DBGRANTS.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          37701 UsersRolesGrants.Grants.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9579 UsersRolesGrants.INTEGRATION.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9301 UsersRolesGrants.NOTIFICATION_SUBSCRIPTION.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          17246 UsersRolesGrants.ROLE.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          16730 UsersRolesGrants.SCHEMA.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM        1333324 UsersRolesGrants.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.html
-a---            2/2/2021  5:08 PM           9309 UsersRolesGrants.STAGE.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          12093 UsersRolesGrants.TABLE.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          12033 UsersRolesGrants.USER.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          66038 UsersRolesGrants.UsersRoles.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM          13331 UsersRolesGrants.VIEW.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
-a---            2/2/2021  5:08 PM           9816 UsersRolesGrants.WAREHOUSE.sfpscogs_dodievich_sso.west-us-2.azure.202102030106.xlsx
```