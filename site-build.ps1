<#
  site-build.ps1

  Generates a single zip file containing the entire tipitaka.org site
   -- all of the static HTML/JS/CSS
   -- all of the split XML files in N scripts

  Run this in PowerShell with a current directory at the root of the tipitaka.org
  git working directory.

  It will create a directory peer to the git directory called tipitaka-site-build.
  The script will only put up one prompt, if that directory exists. If so, delete it first.



  FSnow, April 16, 2020
#>

$buildDir = '..\tipitaka-site-build'

$siteFiles = '.\tipitaka.org'
$rootText = '.\root text files'
$conversionScripts = '.\conversion scripts'

function Get-TimeStamp
{
  return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

# test if the build directory exists
$dirExists = Test-Path $buildDir -PathType Any

# if the directory already exists, prompt to delete it or exit
if ($dirExists) {
  $response = Read-Host -Prompt "$buildDir already exists. Press Y to delete directory and all its contents or any other key to exit"
  if ($response -ieq "y") {
    Write-Host "$(Get-TimeStamp) Deleting existing build directory"
    Remove-Item -path $buildDir -recurse
  }
  else {
    Write-Host "$(Get-TimeStamp) Exiting"
    Exit
  }
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

Write-Host "$(Get-TimeStamp) Splitting root text files and writing into deva\cscd"

# split the root text files and convert to XML, writing into deva\cscd
foreach ($file in Get-ChildItem $rootText\*.txt )
{
  # in PowerShell, ampersand is how you run a command from a string, not to be
  # confused with the ampersand in Unix shells that runs a process in the background
  & "$conversionScripts\pitaka2xml.exe" -split $file $buildDir\deva\cscd
}

# pause here to correct any errors in the split manually
$response = ""
do {
  $response = Read-Host -Prompt "Fix XML split issues manually in $buildDir\deva\cscd then press C to continue"
}
until ($response -ieq "c")

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

  & "$conversionScripts\Deva2Beng.exe" $file $buildDir\beng\cscd
  & "$conversionScripts\Deva2Cyrl.exe" $file $buildDir\cyrl\cscd
  & "$conversionScripts\Deva2Gujr.exe" $file $buildDir\gujr\cscd
  & "$conversionScripts\Deva2Guru.exe" $file $buildDir\guru\cscd
  & "$conversionScripts\Deva2Khmr.exe" $file $buildDir\khmr\cscd
  & "$conversionScripts\Deva2Knda.exe" $file $buildDir\knda\cscd
  & "$conversionScripts\Deva2Mlym.exe" $file $buildDir\mlym\cscd
  # Deva2Latn is the converter but it is the romn directory on site
  & "$conversionScripts\Deva2Latn.exe" $file $buildDir\romn\cscd
  & "$conversionScripts\Deva2Sinh.exe" $file $buildDir\sinh\cscd
  & "$conversionScripts\Deva2Taml.exe" $file $buildDir\taml\cscd
  & "$conversionScripts\Deva2Telu.exe" $file $buildDir\telu\cscd
  & "$conversionScripts\Deva2Thai.exe" $file $buildDir\thai\cscd
  & "$conversionScripts\Deva2Tibt.exe" $file $buildDir\tibt\cscd

  $counter++
}

Write-Host "$(Get-TimeStamp) Creating zip file"

Compress-Archive -Path $buildDir\* -DestinationPath .\tipitaka-site-build

Write-Host "$(Get-TimeStamp) Finished. Zip file tipitaka-site-build.zip should be in same directory as this script."

