<#
  public-xml-build.ps1

  Copies the deva master files to the tipitaka-xml repo and converts them to
  all the supported scripts.

  Run this in PowerShell with a current directory at the root of the tipitaka.org
  git working directory.

  Assumes that the tipitaka-xml and tipitaka.org local repos are under the
  same directory. If not, modify $publicXmlDir. Will error if the tipitaka-xml
  directory does not exist at the expected path ($publicXmlDir).

  FSnow, June 15, 2023
#>

$publicXmlDir = "..\tipitaka-xml"
$pubDevaMaster = "$publicXmlDir\deva master"

$devaMaster = '.\deva master'
$conversionScripts = '.\conversion scripts'


function Get-TimeStamp
{
  return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

# test if the build directory exists
$dirExists = Test-Path $publicXmlDir -PathType Any

# if the directory does not exist, exit
if (-Not $dirExists) {
  Write-Host "$(Get-TimeStamp) tipitaka-xml directory does not exist. Exiting"
  Exit
}

Write-Host "$(Get-TimeStamp) Copying contents of deva master directory"

# copy the contents of deva master to tipitaka-xml\deva master
Copy-Item $devaMaster\* -Recurse -Destination $pubDevaMaster

# to prevent intermittent errors from deleting the TOC files too soon after copying them,
# sleep for 2 seconds
Start-Sleep -s 2

# delete the TOC files
Remove-Item -path $pubDevaMaster\*.toc.xml

# count the deva XML files
$xmlCount = (Get-ChildItem $pubDevaMaster\s01*.xml | Measure-Object).Count

Write-Host "$(Get-TimeStamp) Converting $xmlCount Deva source files into 14 scripts"

$counter = 0

# convert each file in cscd\dev into every other script
foreach ($file in Get-ChildItem $pubDevaMaster\s01*.xml )
{
  if ($counter -gt 0 -and $counter % 10 -eq 0) {
    Write-Host "$(Get-TimeStamp) Converted ${counter} Deva source files of ${xmlCount}"
  }

  & "$conversionScripts\deva2beng.exe" $file $publicXmlDir\beng
  & "$conversionScripts\deva2cyrl.exe" $file $publicXmlDir\cyrl
  & "$conversionScripts\deva2gujr.exe" $file $publicXmlDir\gujr
  & "$conversionScripts\deva2guru.exe" $file $publicXmlDir\guru
  & "$conversionScripts\deva2khmr.exe" $file $publicXmlDir\khmr
  & "$conversionScripts\deva2knda.exe" $file $publicXmlDir\knda
  & "$conversionScripts\deva2mlym.exe" $file $publicXmlDir\mlym
  & "$conversionScripts\deva2mymr.exe" $file $publicXmlDir\mymr
  # Deva2Latn is the converter but it is the romn directory on site
  & "$conversionScripts\deva2latn.exe" $file $publicXmlDir\romn
  & "$conversionScripts\deva2sinh.exe" $file $publicXmlDir\sinh
  & "$conversionScripts\deva2taml.exe" $file $publicXmlDir\taml
  & "$conversionScripts\deva2telu.exe" $file $publicXmlDir\telu
  & "$conversionScripts\deva2thai.exe" $file $publicXmlDir\thai
  & "$conversionScripts\deva2tibt.exe" $file $publicXmlDir\tibt

  $counter++
}

Write-Host "$(Get-TimeStamp) Finished."

