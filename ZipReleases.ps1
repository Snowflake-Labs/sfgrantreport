$zip = "C:\Program Files\7-Zip\7z.exe"

$version = "2021.2.23.0"

cd "C:\snowflake\snowgrantreport\bin\Publish\win"
& $zip a "C:\snowflake\GrantReport\Releases\$version\SnowGrantReport.win.$version.zip" '@C:\snowflake\snowgrantreport\Release\listfile.win.txt'

cd "C:\snowflake\snowgrantreport\bin\Publish\osx"
& $zip a "C:\snowflake\GrantReport\Releases\$version\SnowGrantReport.osx.$version.zip" '@C:\snowflake\snowgrantreport\Release\listfile.osx.txt'

cd "C:\snowflake\snowgrantreport\bin\Publish\linux"
& $zip a "C:\snowflake\GrantReport\Releases\$version\SnowGrantReport.linux.$version.zip" '@C:\snowflake\snowgrantreport\Release\listfile.linux.txt'

cd "C:\snowflake\snowgrantreport\"
