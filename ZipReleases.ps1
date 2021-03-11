$zip = "C:\Program Files\7-Zip\7z.exe"

$version = $projXML.SelectNodes("Project/PropertyGroup/Version")."#text"
$version

cd "bin\Publish\win"
& $zip a "..\..\..\..\Releases\$version\SFGrantReport.win.$version.zip" '@..\..\..\Release\listfile.win.txt'

cd ..\..\..
cd "\bin\Publish\osx"
#& $zip a "C:\snowflake\GrantReport\Releases\$version\SFGrantReport.osx.$version.zip" '@C:\snowflake\snowgrantreport\Release\listfile.osx.txt'

cd ..\..\..
cd "\bin\Publish\linux"
#& $zip a "C:\snowflake\GrantReport\Releases\$version\SFGrantReport.linux.$version.zip" '@C:\snowflake\snowgrantreport\Release\listfile.linux.txt'

cd ..\..\..
