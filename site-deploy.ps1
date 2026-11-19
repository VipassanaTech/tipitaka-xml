<#
  site-deploy.ps1

  Deploys to tipitaka.org whatever changed in git since the commit the site was last
  deployed from. That commit is read from version.txt on the server, or given with -Base.

  - Static files: added or changed files under tipitaka.org/ are uploaded, and files
    deleted in git are deleted on the server. Generated XML (tipitaka.org/<script>/cscd/*.xml)
    is never taken from git; the build makes it.
  - Books: each book whose "deva master/<book>.xml" changed is split with DevaMaster2Split
    and its files are converted into the 13 other scripts. Files the old version of the book
    produced but the new one does not are deleted from all 14 scripts. The old file names come
    from splitting the old version of the book with the old commit's splitter, so they are the
    names that were actually deployed. Changes to "deva master/*.toc.xml" are ignored: the
    site's tables of contents are tree.json, and the splitter writes its own .toc.xml.
  - Tools: if DevaMaster2Split.exe changed, every book is rebuilt in every script. If a
    converter's .exe (or .exe.config) changed or was added, every book is rebuilt in that
    script only; a new script such as orya is deployed in full this way. A script is built
    only at commits where its converter exists. Other tools in "conversion scripts" do not
    affect the site.
  - version.txt (commit and date) is uploaded last. If a run fails partway, the next run
    starts again from the old commit and redoes the whole difference.

  Tools are always taken from git at the commit being built, not from the working tree, so
  the result depends only on the two commits.

  Uploading uses rclone. -Remote is an rclone remote path such as "site:/var/www/tipitaka.org";
  the remote itself is configured by the caller (the GitHub workflow does it with RCLONE_CONFIG_*
  environment variables). Without -Remote, or with -DryRun, nothing is uploaded or deleted:
  the files that would be uploaded are left in <OutDir>\upload and the plan in <OutDir>.

  Examples:
    .\site-deploy.ps1 -Base site-staging-2026-11-11 -DryRun
    .\site-deploy.ps1 -Remote site:/var/www/tipitaka.org
#>

[CmdletBinding()]
param(
  # commit the site was deployed from; default: read version.txt from -Remote
  [string]$Base,
  # commit to deploy
  [string]$Head = 'HEAD',
  # rclone remote path of the site root
  [string]$Remote,
  # working directory; deleted and recreated on each run
  [string]$OutDir = (Join-Path ([IO.Path]::GetTempPath()) 'tipitaka-site-deploy'),
  # rebuild every book even if the tools did not change
  [switch]$Full,
  # build and plan, but do not upload or delete anything
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$repo = $PSScriptRoot

# site directory => converter. deva is the splitter's own output. A script is built only
# at commits where its converter exists, so a converter can be added in its own commit.
$allConverters = [ordered]@{
  beng = 'deva2beng'; cyrl = 'deva2cyrl'; gujr = 'deva2gujr'; guru = 'deva2guru'
  khmr = 'deva2khmr'; knda = 'deva2knda'; mlym = 'deva2mlym'; mymr = 'deva2mymr'
  orya = 'deva2orya'; romn = 'deva2latn'; sinh = 'deva2sinh'; telu = 'deva2telu'
  thai = 'deva2thai'; tibt = 'deva2tibt'
}

function Write-Step([string]$message) {
  Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $message)
}

# Runs a native command and returns its output lines. Throws if it exits non-zero.
function Invoke-Native([string]$exe, [string[]]$arguments) {
  $saved = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try {
    $out = & $exe @arguments 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
  } finally {
    $ErrorActionPreference = $saved
  }
  if ($code -ne 0) {
    throw ("{0} {1} exited with code {2}:`n{3}" -f $exe, ($arguments -join ' '), $code, ($out -join "`n"))
  }
  return $out
}

function Invoke-Git([string[]]$arguments) {
  return Invoke-Native 'git' (@('-C', $repo, '-c', 'core.autocrlf=false', '-c', 'core.quotepath=false') + $arguments)
}

# Writes the file at <rev>:<path> to $dest byte for byte, as a checkout would write it.
# (PowerShell pipes and redirection would re-encode the bytes.)
function Save-GitFile([string]$rev, [string]$path, [string]$dest) {
  New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
  $psi = New-Object System.Diagnostics.ProcessStartInfo 'git'
  $psi.Arguments = ('-C "{0}" -c core.autocrlf=false cat-file --filters "{1}:{2}"' -f $repo, $rev, $path)
  $psi.UseShellExecute = $false
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $p = [System.Diagnostics.Process]::Start($psi)
  $stderr = $p.StandardError.ReadToEndAsync()
  $fs = [IO.File]::Create($dest)
  try { $p.StandardOutput.BaseStream.CopyTo($fs) } finally { $fs.Close() }
  $p.WaitForExit()
  if ($p.ExitCode -ne 0) {
    throw "git cat-file ${rev}:$path failed: $($stderr.Result)"
  }
}

function Test-GitPath([string]$rev, [string]$path) {
  $saved = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try {
    & git -C $repo cat-file -e "${rev}:$path" 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
  } finally {
    $ErrorActionPreference = $saved
  }
}

# Book file names (e.g. s0101m.mul.xml) in "deva master" at a commit
function Get-Books([string]$rev) {
  Invoke-Git @('ls-tree', '--name-only', $rev, '--', 'deva master/') |
    ForEach-Object { $_.Substring('deva master/'.Length) } |
    Where-Object { $_ -like '*.xml' -and $_ -notlike '*.toc.xml' }
}

# The converters (site directory => converter) whose exe exists at a commit
function Get-Converters([string]$rev) {
  $c = [ordered]@{}
  foreach ($s in $allConverters.Keys) {
    if (Test-GitPath $rev "conversion scripts/$($allConverters[$s]).exe") { $c[$s] = $allConverters[$s] }
  }
  return $c
}

# Extracts the splitter, and the converters with any .exe.config they have, at a commit into $dir
function Save-Tools([string]$rev, $converters, [string]$dir) {
  if (-not (Test-GitPath $rev 'conversion scripts/DevaMaster2Split.exe')) {
    throw "conversion scripts/DevaMaster2Split.exe does not exist at $rev"
  }
  Save-GitFile $rev 'conversion scripts/DevaMaster2Split.exe' (Join-Path $dir 'DevaMaster2Split.exe')
  foreach ($name in $converters.Values) {
    Save-GitFile $rev "conversion scripts/$name.exe" (Join-Path $dir "$name.exe")
    if (Test-GitPath $rev "conversion scripts/$name.exe.config") {
      Save-GitFile $rev "conversion scripts/$name.exe.config" (Join-Path $dir "$name.exe.config")
    }
  }
}

# The book a split file belongs to: s0101m.mul12.xml and s0101m.mul.toc.xml => s0101m.mul.xml
function Get-BookOf([string]$fileName) {
  if ($fileName -match '^(.+)\.toc\.xml$') { return "$($Matches[1]).xml" }
  if ($fileName -match '^(.+\.[a-z]+)\d*\.xml$') { return "$($Matches[1]).xml" }
  throw "Cannot tell which book $fileName belongs to"
}

# Splits the given books as of $rev with the splitter in $toolDir. Returns the output lines.
function Split-Books([string]$rev, [string[]]$books, [string]$toolDir, [string]$workDir, [string]$outputDir) {
  $inDir = Join-Path $workDir 'books'
  New-Item -ItemType Directory -Force $inDir, $outputDir | Out-Null
  foreach ($b in $books) {
    Save-GitFile $rev "deva master/$b" (Join-Path $inDir $b)
  }
  if ($books.Count -eq 0) { return @() }
  return Invoke-Native (Join-Path $toolDir 'DevaMaster2Split.exe') @($inDir, $outputDir)
}

# ---------------------------------------------------------------------------------------
# commits

$headSha = (Invoke-Git @('rev-parse', "$Head^{commit}")) | Select-Object -First 1

if (-not $Base) {
  if (-not $Remote) { throw 'Give -Base, or -Remote to read version.txt from the server' }
  $saved = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  $version = & rclone cat "$Remote/version.txt" 2>&1 | ForEach-Object { "$_" }
  $code = $LASTEXITCODE
  $ErrorActionPreference = $saved
  if ($code -ne 0) {
    throw "Could not read $Remote/version.txt (give -Base for the first deploy):`n$($version -join "`n")"
  }
  $Base = ($version | Select-Object -First 1).Trim()
  Write-Step "Server version.txt: $Base"
}
$baseSha = (Invoke-Git @('rev-parse', "$Base^{commit}")) | Select-Object -First 1

Write-Step "Deploying $($headSha.Substring(0,7)) over $($baseSha.Substring(0,7))"

# ---------------------------------------------------------------------------------------
# what changed

$raw = (Invoke-Git @('diff', '--name-status', '--no-renames', '-z', $baseSha, $headSha)) -join "`n"
$fields = @($raw.Split([char]0) | ForEach-Object { $_.Trim("`n") } | Where-Object { $_ -ne '' })

$staticUpload = New-Object System.Collections.Generic.List[string]
$staticDelete = New-Object System.Collections.Generic.List[string]
$changedBooks = New-Object System.Collections.Generic.SortedSet[string]
$splitterChanged = $false
$changedConverters = New-Object System.Collections.Generic.SortedSet[string]   # site directories

for ($i = 0; $i -lt $fields.Count; $i += 2) {
  $status = $fields[$i].Substring(0, 1)
  $path = $fields[$i + 1]

  if ($path -match '^deva master/([^/]+\.xml)$' -and $path -notlike '*.toc.xml') {
    [void]$changedBooks.Add($Matches[1])
  }
  elseif ($path -eq 'conversion scripts/DevaMaster2Split.exe') {
    $splitterChanged = $true
  }
  elseif ($path -match '^conversion scripts/([^/]+)\.exe(\.config)?$') {
    # only the site's converters; other tools (DevaNormalize.exe, ...) do not affect the site
    $name = $Matches[1]
    foreach ($s in $allConverters.Keys) { if ($allConverters[$s] -eq $name) { [void]$changedConverters.Add($s) } }
  }
  elseif ($path -match '^tipitaka\.org/(.+)$') {
    $rel = $Matches[1]
    if ($rel -match '^[^/]+/cscd/[^/]+\.xml$') { continue }   # generated; the build makes these
    if ($status -eq 'D') { $staticDelete.Add($rel) } else { $staticUpload.Add($rel) }
  }
}

$headBooks = @(Get-Books $headSha)
$baseBooks = @(Get-Books $baseSha)
$converters = Get-Converters $headSha
$scriptDirs = @('deva') + @($converters.Keys)

# Scripts that get every book: all of them if the splitter changed, else those whose
# converter changed (or was added). Changed books are rebuilt in every script.
$fullScripts = New-Object System.Collections.Generic.SortedSet[string]
if ($Full -or $splitterChanged) {
  Write-Step $(if ($Full) { 'Rebuilding every book (-Full)' } else { 'The splitter changed: rebuilding every book' })
  foreach ($b in $headBooks + $baseBooks) { [void]$changedBooks.Add($b) }
  foreach ($s in $scriptDirs) { [void]$fullScripts.Add($s) }
}
foreach ($s in $changedConverters) {
  if ($converters.Contains($s)) { [void]$fullScripts.Add($s) }
}
if ($fullScripts.Count -gt 0 -and -not ($Full -or $splitterChanged)) {
  Write-Step "Converter changed: rebuilding every book in $(@($fullScripts) -join ', ')"
}

$newBooks = @($changedBooks | Where-Object { $headBooks -contains $_ })
$oldBooks = @($changedBooks | Where-Object { $baseBooks -contains $_ })
$splitBooks = @(if ($fullScripts.Count -gt 0) { $headBooks } else { $newBooks })

Write-Step ("Changed: {0} books, {1} scripts with every book, {2} static files to upload, {3} to delete" -f `
  $changedBooks.Count, $fullScripts.Count, $staticUpload.Count, $staticDelete.Count)

# ---------------------------------------------------------------------------------------
# build

if (Test-Path $OutDir) {
  if (-not (Test-Path (Join-Path $OutDir '.site-deploy'))) {
    throw "$OutDir exists but was not made by this script; give another -OutDir"
  }
  Remove-Item -Recurse -Force $OutDir
}
$upload = Join-Path $OutDir 'upload'      # site layout: exactly the files to upload
$work = Join-Path $OutDir 'work'
New-Item -ItemType Directory -Force $upload, $work | Out-Null
Set-Content (Join-Path $OutDir '.site-deploy') 'working directory of site-deploy.ps1' -Encoding ASCII

$bookDelete = New-Object System.Collections.Generic.List[string]
$splitLog = @()

if ($splitBooks.Count -gt 0 -or $oldBooks.Count -gt 0) {
  $headTools = Join-Path $work 'tools-head'
  Save-Tools $headSha $converters $headTools

  # split
  $splitDir = Join-Path $work 'split'
  Write-Step "Splitting $($splitBooks.Count) books"
  $splitLog = @(Split-Books $headSha $splitBooks $headTools (Join-Path $work 'head') $splitDir)
  $splitLog | ForEach-Object { Write-Host "  $_" }
  $splitFiles = @(Get-ChildItem $splitDir -Filter *.xml -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -eq '.xml' } | Sort-Object Name)

  # the files each script gets: every file in $fullScripts, else the changed books' files
  $newBookSet = [Collections.Generic.HashSet[string]]::new([string[]]$newBooks)
  $changedFiles = @($splitFiles | Where-Object { $newBookSet.Contains((Get-BookOf $_.Name)) })
  $filesFor = @{}
  foreach ($s in $scriptDirs) {
    $filesFor[$s] = @(if ($fullScripts.Contains($s)) { $splitFiles } else { $changedFiles })
  }

  $devaOut = Join-Path $upload 'deva\cscd'
  New-Item -ItemType Directory -Force $devaOut | Out-Null
  foreach ($f in $filesFor['deva']) { Copy-Item -LiteralPath $f.FullName $devaOut }

  # file names the old versions produced
  $oldNames = @()
  if ($oldBooks.Count -gt 0) {
    $baseTools = Join-Path $work 'tools-base'
    Save-Tools $baseSha ([ordered]@{}) $baseTools
    $oldOut = Join-Path $work 'base-split'
    Write-Step "Splitting the $($oldBooks.Count) old versions for their file names"
    Split-Books $baseSha $oldBooks $baseTools (Join-Path $work 'base') $oldOut | Out-Null
    $oldNames = @(Get-ChildItem $oldOut -Filter *.xml | Where-Object { $_.Extension -eq '.xml' } | ForEach-Object Name)
  }
  $newNames = [Collections.Generic.HashSet[string]]::new([string[]]@($splitFiles | ForEach-Object Name))
  foreach ($n in $oldNames) {
    if (-not $newNames.Contains($n)) {
      foreach ($s in $scriptDirs) { $bookDelete.Add("$s/cscd/$n") }
    }
  }

  # convert: one background job per script, so the converters run in parallel.
  # Each converter handles one file per run, so the time is mostly process start-up.
  # At most two jobs per CPU: 13 at once ran Merlin (2 CPUs, 4 GB) out of memory.
  $toConvert = @($converters.Keys | Where-Object { $filesFor[$_].Count -gt 0 })
  $maxJobs = [Math]::Max(2, 2 * [Environment]::ProcessorCount)
  Write-Step ("Converting into {0} scripts, {1} at a time: {2}" -f $toConvert.Count, $maxJobs,
    (($toConvert | ForEach-Object { "$_ $($filesFor[$_].Count)" }) -join ', '))
  $jobs = foreach ($s in $toConvert) {
    while (@(Get-Job | Where-Object { $_.State -eq 'Running' }).Count -ge $maxJobs) {
      Wait-Job -Any (Get-Job | Where-Object { $_.State -eq 'Running' }) | Out-Null
    }
    $exe = Join-Path $headTools "$($converters[$s]).exe"
    $outDirS = Join-Path $upload "$s\cscd"
    New-Item -ItemType Directory -Force $outDirS | Out-Null
    $paths = [string[]]@($filesFor[$s] | ForEach-Object FullName)
    Start-Job -Name $s -ArgumentList @($exe, $outDirS, $paths) -ScriptBlock {
      param($exe, $outDir, $files)
      $ErrorActionPreference = 'Continue'
      foreach ($file in $files) {
        $out = @(& $exe $file $outDir 2>&1 | ForEach-Object { "$_" })
        $target = Join-Path $outDir (Split-Path $file -Leaf)
        # the converters print errors and still exit 0, so check their output too
        if ($LASTEXITCODE -ne 0 -or $out.Count -gt 0 -or -not (Test-Path $target) -or (Get-Item $target).Length -eq 0) {
          "FAILED $(Split-Path $exe -Leaf) on $(Split-Path $file -Leaf) (exit $LASTEXITCODE): $($out -join ' ')"
          return
        }
      }
    }
  }
  $jobs = @($jobs | Where-Object { $_ })
  if ($jobs.Count -gt 0) {
    $jobs | Wait-Job | Out-Null
    $failures = @($jobs | ForEach-Object {
      if ($_.State -ne 'Completed') { "job $($_.Name) ended $($_.State): $($_.ChildJobs[0].JobStateInfo.Reason)" }
      Receive-Job $_
    } | Where-Object { $_ })
    $jobs | Remove-Job
    if ($failures.Count -gt 0) { throw "Conversion failed:`n$($failures -join "`n")" }
  }

  # every script has exactly the files it should
  foreach ($s in $scriptDirs) {
    $dir = Join-Path $upload "$s\cscd"
    $have = @(Get-ChildItem $dir -Filter *.xml -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.xml' } |
      ForEach-Object Name | Sort-Object)
    $want = @($filesFor[$s] | ForEach-Object Name | Sort-Object)
    if (($have -join '|') -ne ($want -join '|')) { throw "$s has $($have.Count) files, it should have $($want.Count)" }
  }
}

# static files
foreach ($rel in $staticUpload) {
  Save-GitFile $headSha "tipitaka.org/$rel" (Join-Path $upload ($rel -replace '/', '\'))
}

# version.txt, uploaded on its own after everything else
$versionFile = Join-Path $OutDir 'version.txt'
$date = (Invoke-Git @('show', '-s', '--format=%cI', $headSha)) | Select-Object -First 1
[IO.File]::WriteAllText($versionFile, "$headSha`n$date`n", (New-Object Text.UTF8Encoding $false))

# ---------------------------------------------------------------------------------------
# plan

$uploadList = @(Get-ChildItem $upload -Recurse -File | ForEach-Object {
  $_.FullName.Substring($upload.Length + 1).Replace('\', '/') } | Sort-Object)
$deleteList = @(@($bookDelete) + @($staticDelete) | Sort-Object -Unique)
$utf8 = New-Object Text.UTF8Encoding $false
[IO.File]::WriteAllLines((Join-Path $OutDir 'uploads.txt'), [string[]]$uploadList, $utf8)
[IO.File]::WriteAllLines((Join-Path $OutDir 'deletes.txt'), [string[]]$deleteList, $utf8)
# a record of exactly what this deploy uploaded
$hashes = @($uploadList | ForEach-Object {
  "{0}`t{1}" -f (Get-FileHash -LiteralPath (Join-Path $upload ($_ -replace '/', '\')) -Algorithm SHA256).Hash, $_ })
[IO.File]::WriteAllLines((Join-Path $OutDir 'upload-hashes.tsv'), [string[]]$hashes, $utf8)

$summary = @(
  "### tipitaka.org deploy $($headSha.Substring(0,7)) over $($baseSha.Substring(0,7))"
  ''
  "- Books rebuilt: $($changedBooks.Count)$(if ($changedBooks.Count -gt 0 -and $changedBooks.Count -le 20) { ' (' + (@($changedBooks) -join ', ') + ')' })"
  "- Files to upload: $($uploadList.Count) ($($staticUpload.Count) static)"
  "- Files to delete: $($deleteList.Count)"
  "- Normalized by the splitter: $(@($splitLog | Where-Object { $_ -match 'normalized' }).Count) books (deva master still needs DevaNormalize)"
  "- Mode: $(if ($DryRun -or -not $Remote) { 'dry run, nothing uploaded' } else { "upload to $Remote" })"
)
$summary | ForEach-Object { Write-Host $_ }
if ($env:GITHUB_STEP_SUMMARY) { $summary | Add-Content $env:GITHUB_STEP_SUMMARY -Encoding UTF8 }

if ($DryRun -or -not $Remote) {
  Write-Step "Dry run: files to upload are in $upload; plan in $OutDir\uploads.txt and deletes.txt"
  return
}

# ---------------------------------------------------------------------------------------
# deploy

if ($uploadList.Count -gt 0) {
  Write-Step "Uploading $($uploadList.Count) files"
  Invoke-Native 'rclone' @('copy', $upload, $Remote, '--no-check-dest', '--transfers', '8',
    '--retries', '5', '--stats', '30s', '--stats-one-line', '-v') | ForEach-Object { Write-Host "  $_" }
}

if ($deleteList.Count -gt 0) {
  # one rclone run over a few connections; each listed path is looked up directly, nothing
  # else on the server is listed or touched, and a file that is already gone is skipped
  Write-Step "Deleting $($deleteList.Count) files"
  Invoke-Native 'rclone' @('delete', $Remote, '--files-from-raw', (Join-Path $OutDir 'deletes.txt'),
    '--checkers', '8', '--retries', '5', '-v') | ForEach-Object { Write-Host "  $_" }
}

Write-Step 'Uploading version.txt'
Invoke-Native 'rclone' @('copyto', $versionFile, "$Remote/version.txt", '--no-check-dest', '--retries', '5') |
  ForEach-Object { Write-Host "  $_" }

Write-Step "Deployed $($headSha.Substring(0,7))"
