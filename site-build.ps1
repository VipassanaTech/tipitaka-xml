<#
  site-build.ps1

  Generates a single zip file containing the entire tipitaka.org site
   -- all of the static HTML/JS/CSS
   -- all of the split XML files in N scripts

  Run this in PowerShell with a current directory at the root of the tipitaka.org
  git working directory.

  It will create a directory peer to the git directory called tipitaka-site-build.
  If that directory already exists, it is deleted first. The script does not prompt,
  so it can run unattended. It exits with code 1 if any book cannot be split.

  The Devanagari XML is split from the books in "deva master" by DevaMaster2Split.exe
  (VipassanaTech/tipitaka-xml#2679). The text files in "root text files" are no longer
  used.



  FSnow, April 16, 2020
#>

$buildDir = '..\tipitaka-site-build'

$siteFiles = '.\tipitaka.org'
$devaMaster = '.\deva master'
$conversionScripts = '.\conversion scripts'

function Get-TimeStamp
{
  return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

# test if the build directory exists
$dirExists = Test-Path $buildDir -PathType Any

# if the directory already exists, delete it. It holds only the output of the last build.
if ($dirExists) {
  Write-Host "$(Get-TimeStamp) Deleting existing build directory"
  Remove-Item -path $buildDir -recurse
}

# to prevent intermittent errors from creating the directory too soon after deleting it,
# sleep for 2 seconds
Start-Sleep -s 2

Write-Host "$(Get-TimeStamp) Creating build directory .\tipitaka-site-build"

# create the empty build directory
New-Item $buildDir -type directory

Write-Host "$(Get-TimeStamp) Copying contents of tipitaka.org directory"

# copy the contents of the tipitaka.org directory (the child directory of the root
# that contains the home page and all of the script directories, deva, etc.)
Copy-Item $siteFiles\* -Recurse -Destination $buildDir

# delete the XML files from deva\cscd that were checked in to source control
Remove-Item -path $buildDir\deva\cscd\*.xml

Write-Host "$(Get-TimeStamp) Splitting deva master books and writing into deva\cscd"

# split every book in deva master into its chapter files and .toc.xml, writing into deva\cscd.
# in PowerShell, ampersand is how you run a command from a string, not to be
# confused with the ampersand in Unix shells that runs a process in the background
& "$conversionScripts\DevaMaster2Split.exe" $devaMaster $buildDir\deva\cscd

# stop rather than publish a site with books missing. The splitter has already
# printed which books failed and why.
if ($LASTEXITCODE -ne 0) {
  Write-Host "$(Get-TimeStamp) Splitting failed. Exiting"
  Exit 1
}

# count the split deva XML files
$splitCount = (Get-ChildItem $buildDir\deva\cscd\*.xml | Measure-Object).Count

Write-Host "$(Get-TimeStamp) Converting ${splitCount} split deva files into other scripts"

$counter = 0

# convert each file in cscd\dev into every other script
foreach ($file in Get-ChildItem $buildDir\deva\cscd\*.xml )
{
  if ($counter -gt 0 -and $counter % 100 -eq 0) {
    Write-Host "$(Get-TimeStamp) Converted ${counter} files of ${splitCount}"
  }

  & "$conversionScripts\deva2beng.exe" $file $buildDir\beng\cscd
  & "$conversionScripts\deva2cyrl.exe" $file $buildDir\cyrl\cscd
  & "$conversionScripts\deva2gujr.exe" $file $buildDir\gujr\cscd
  & "$conversionScripts\deva2guru.exe" $file $buildDir\guru\cscd
  & "$conversionScripts\deva2khmr.exe" $file $buildDir\khmr\cscd
  & "$conversionScripts\deva2knda.exe" $file $buildDir\knda\cscd
  & "$conversionScripts\deva2mlym.exe" $file $buildDir\mlym\cscd
  & "$conversionScripts\deva2mymr.exe" $file $buildDir\mymr\cscd
  # Deva2Latn is the converter but it is the romn directory on site
  & "$conversionScripts\deva2latn.exe" $file $buildDir\romn\cscd
  & "$conversionScripts\deva2sinh.exe" $file $buildDir\sinh\cscd
  & "$conversionScripts\deva2telu.exe" $file $buildDir\telu\cscd
  & "$conversionScripts\deva2thai.exe" $file $buildDir\thai\cscd
  & "$conversionScripts\deva2tibt.exe" $file $buildDir\tibt\cscd

  $counter++
}

Write-Host "$(Get-TimeStamp) Creating zip file"

# -Force overwrites the zip from the previous build
Compress-Archive -Path $buildDir\* -DestinationPath .\tipitaka-site-build -Force

Write-Host "$(Get-TimeStamp) Finished. Zip file tipitaka-site-build.zip should be in same directory as this script."

