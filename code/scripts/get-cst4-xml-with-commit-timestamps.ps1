<#
 get-cst4-xml-with-commit-timestamps.ps1

  Copies the 217 CST4 XML files from "deva master" into a directory,
 then gets the commit timestamp for each of the files and inserts it into
 /TEI.2/teiHeader/fileDesc/publicationStmt/date/@when.

  Run this in PowerShell with a current directory of the code/scripts directory.

  It will create a directory peer to the git directory called cst4-xml.
  The script will only put up one prompt, if that directory exists. If so, delete it first.


  FSnow, June 26, 2021
#>

$buildDirUp3 = '..\..\..\cst4-xml'
$buildDirUp2 = '..\..\cst4-xml'

function Get-TimeStamp
{
  return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

# test if the build directory exists
$dirExists = Test-Path $buildDirUp3 -PathType Any

# if the directory already exists, prompt to delete it or exit
if ($dirExists) {
  $response = Read-Host -Prompt "$buildDirUp3 already exists. Press Y to delete directory and all its contents or any other key to exit"
  if ($response -ieq "y") {
    Write-Host "$(Get-TimeStamp) Deleting existing directory"
    Remove-Item -path $buildDirUp3 -recurse
  }
  else {
    Write-Host "$(Get-TimeStamp) Exiting"
    Exit
  }
}

# to prevent intermittent errors from creating the directory too soon after deleting it,
# sleep for 2 seconds
Start-Sleep -s 2

Write-Host "$(Get-TimeStamp) Creating directory cst4-xml, peer to repo root"

# create the empty output directory
New-Item $buildDirUp3 -type directory

cd ..\..\'deva master'

# copy the CST4 XML files to the output directory
#Copy-Item $siteFiles\* -Recurse -Destination $buildDir

foreach ($file in Get-ChildItem | Where-Object { $_.Name -match '^.*[mul|att|tik|nrf]\.xml' } ) 
{
    #Copy-Item $file -Destination $buildDirUp2
    $gitts = (git log -1 --format=%cd --date=iso-strict $file.Name)
    Write-Host "$file, $gitts"
    $replacement = "<date when=`"$($gitts)`"/>"
    ((Get-Content -path $file -Raw -Encoding unicode) -replace '<date/>', $replacement) | Set-Content -Encoding unicode -Path $buildDirUp2\$file
}

cd ..\code\scripts
