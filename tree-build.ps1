<#
  tree-build.ps1

  Updates the navigation trees of tipitaka.org, tipitaka.org/<script>/tree.json,
  to match the chapter files that DevaMaster2Split.exe makes from "deva master"
  (VipassanaTech/tipitaka-xml#2726). Run it by hand after a change that adds,
  removes, merges or renames chapters, review what it prints, and commit the
  changed tree.json files with that change. The deploy uploads them.

  1. DevaMaster2Split.exe splits "deva master" into a temporary directory.
  2. DevaTree.exe updates tipitaka.org/deva/tree.json from the split files' .toc.xml.
     Every node keeps its id, which is in the site's URLs; chapters the tree does
     not have get new ids. It prints every change.
  3. The other scripts' trees are made from the Devanagari tree: its titles are
     converted by each script's deva2*.exe, and put into a copy of it. Only
     scripts that already have a tree.json are updated.

  Run this in PowerShell with the current directory at the root of the
  repository. It exits with code 1 if any step fails; the trees written before
  that step are left as they are.
#>

param(
  # where the .exe tools are
  [string]$Tools = '.\conversion scripts'
)

$ErrorActionPreference = 'Stop'

$site = Join-Path '.' 'tipitaka.org'
$devaMaster = Join-Path '.' 'deva master'
$devaTree = Join-Path $site (Join-Path 'deva' 'tree.json')

# site folder => converter, as in site-deploy.ps1
$converters = [ordered]@{
  beng = 'deva2beng'; cyrl = 'deva2cyrl'; gujr = 'deva2gujr'; guru = 'deva2guru'
  khmr = 'deva2khmr'; knda = 'deva2knda'; mlym = 'deva2mlym'; mymr = 'deva2mymr'
  orya = 'deva2orya'; romn = 'deva2latn'; sinh = 'deva2sinh'; telu = 'deva2telu'
  thai = 'deva2thai'; tibt = 'deva2tibt'
}

function Get-TimeStamp {
  return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

# Runs a tool and stops if it exits non-zero.
function Invoke-Tool([string]$name, [string[]]$arguments) {
  & (Join-Path $Tools "$name.exe") @arguments
  if ($LASTEXITCODE -ne 0) {
    Write-Host "$(Get-TimeStamp) $name failed (exit code $LASTEXITCODE)"
    exit 1
  }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ('tree-build-' + [guid]::NewGuid().ToString('N'))
$split = Join-Path $work 'split'
New-Item -ItemType Directory -Path $split | Out-Null

try {
  Write-Host "$(Get-TimeStamp) Splitting deva master"
  Invoke-Tool 'DevaMaster2Split' @($devaMaster, $split) | Out-Null

  Write-Host "$(Get-TimeStamp) Updating $devaTree"
  Invoke-Tool 'DevaTree' @($devaTree, $split, $devaTree)

  $labels = Join-Path $work 'labels.xml'
  Invoke-Tool 'DevaTree' @('labels', $devaTree, $labels) | Out-Null

  foreach ($dir in $converters.Keys) {
    $tree = Join-Path $site (Join-Path $dir 'tree.json')
    if (-not (Test-Path $tree)) {
      Write-Host "$(Get-TimeStamp) $dir has no tree.json: skipped"
      continue
    }
    $out = Join-Path $work $dir
    New-Item -ItemType Directory -Path $out | Out-Null
    # the converters print their errors but exit 0, so check that the output is there
    Invoke-Tool $converters[$dir] @($labels, $out)
    $converted = Join-Path $out 'labels.xml'
    if (-not (Test-Path $converted)) {
      Write-Host "$(Get-TimeStamp) $($converters[$dir]) did not convert the titles"
      exit 1
    }
    Invoke-Tool 'DevaTree' @('apply', $devaTree, $converted, $dir, $tree)
  }
  Write-Host "$(Get-TimeStamp) Done. Review the changes and commit the tree.json files."
}
finally {
  Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
