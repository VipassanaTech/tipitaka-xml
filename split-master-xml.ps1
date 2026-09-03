param(
    [string]$sourceDir,
    [string]$destDir
)

# Ensure destination directory exists
if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir | Out-Null
}

# Get all main xml files (excluding .toc.xml)
$files = Get-ChildItem -Path $sourceDir -Filter "*.xml" | Where-Object { -not $_.Name.EndsWith(".toc.xml") }
$files = $files | Sort-Object Name

# Define block tags
$blockTags = @{
    "p" = $true
    "head" = $true
    "trailer" = $true
}

# Helper function to get clean plain text from an XML element, excluding <note> elements
function Get-CleanText {
    param($node)
    
    $parts = New-Object System.Collections.Generic.List[string]
    
    function Recurse-Node {
        param($n)
        if ($n.Name -eq 'note') {
            return
        }
        if ($n.NodeType -eq [System.Xml.XmlNodeType]::Text) {
            [void]$parts.Add($n.Value)
        }
        foreach ($child in $n.ChildNodes) {
            Recurse-Node $child
        }
    }
    
    Recurse-Node $node
    
    $text = $parts -join ""
    $text = $text -replace '\s+', ' '
    return $text.Trim()
}

# Helper function to recursively collect block elements from body
function Collect-Blocks {
    param($node, $blocks)
    
    if ($blockTags.ContainsKey($node.Name)) {
        [void]$blocks.Add($node)
        return
    }
    foreach ($child in $node.ChildNodes) {
        Collect-Blocks $child $blocks
    }
}

$totalFiles = $files.Count
Write-Host "Found $totalFiles master files to split."

$counter = 0
foreach ($file in $files) {
    $srcPath = $file.FullName
    $stem = $file.BaseName # e.g. s0101m.mul
    
    # Load xml
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($srcPath)
    
    $body = $xml.SelectSingleNode("//body")
    if ($body -eq $null) {
        Write-Warning "No body element found in $($file.Name)"
        continue
    }
    
    $blocks = New-Object System.Collections.ArrayList
    Collect-Blocks $body $blocks
    
    if ($blocks.Count -eq 0) {
        continue
    }
    
    # Identify title index
    $titleLookAhead = [Math]::Min($blocks.Count, 25)
    $titleIndex = -1
    for ($i = $titleLookAhead - 1; $i -ge 0; $i--) {
        $rend = $blocks[$i].Attributes["rend"]
        $rendVal = if ($rend -ne $null) { $rend.Value } else { "" }
        
        if ($rendVal -eq 'chapter') {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'centrebold' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'subsubhead' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'nikaya' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'book' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'title' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
        elseif ($rendVal -eq 'subhead' -and $titleIndex -eq -1) {
            $titleIndex = $i
        }
    }
    if ($titleIndex -eq -1) {
        $titleIndex = 0
    }
    
    # Create fragments
    $fragments = New-Object System.Collections.ArrayList
    $frag = [PSCustomObject]@{
        title = (Get-CleanText $blocks[$titleIndex])
        start_idx = 0
        end_idx = $null
        filename = $null
    }
    
    for ($i = $titleIndex + 1; $i -lt $blocks.Count; $i++) {
        $rend = $blocks[$i].Attributes["rend"]
        $rendVal = if ($rend -ne $null) { $rend.Value } else { "" }
        if ($rendVal -eq 'chapter') {
            $frag.end_idx = $i - 1
            [void]$fragments.Add($frag)
            
            $frag = [PSCustomObject]@{
                title = (Get-CleanText $blocks[$i])
                start_idx = $i
                end_idx = $null
                filename = $null
            }
        }
    }
    $frag.end_idx = $blocks.Count - 1
    [void]$fragments.Add($frag)
    
    # Assign filenames
    $numFrags = $fragments.Count
    for ($idx = 0; $idx -lt $numFrags; $idx++) {
        $f_info = $fragments[$idx]
        if ($numFrags -gt 1) {
            $f_info.filename = "$stem$idx.xml"
        } else {
            $f_info.filename = "$stem.xml"
        }
    }
    
    # Write split files
    for ($idx = 0; $idx -lt $numFrags; $idx++) {
        $f_info = $fragments[$idx]
        $outputPath = Join-Path $destDir $f_info.filename
        
        $sw = New-Object System.IO.StreamWriter($outputPath, $false, [System.Text.Encoding]::BigEndianUnicode)
        $sw.NewLine = "`r`n"
        $sw.WriteLine("<?xml version=`"1.0`" encoding=`"UTF-16`"?>")
        $sw.WriteLine("<?xml-stylesheet type=`"text/xsl`" href=`"tipitaka-deva.xsl`"?>")
        $sw.WriteLine("<TEI.2>")
        $sw.WriteLine("<teiHeader></teiHeader>")
        $sw.WriteLine("<text>")
        $sw.WriteLine("<front></front>")
        $sw.WriteLine("<body>")
        
        for ($idx_block = $f_info.start_idx; $idx_block -le $f_info.end_idx; $idx_block++) {
            $el = $blocks[$idx_block]
            
            # Form XML string for block
            $tagName = $el.Name
            if ($tagName -eq 'head' -or $tagName -eq 'trailer') {
                $tagName = 'p'
            }
            
            $attrStr = ""
            if ($el.Attributes -ne $null) {
                foreach ($attr in $el.Attributes) {
                    $attrStr += " $($attr.Name)=`"$($attr.Value)`""
                }
            }
            
            $innerXml = $el.InnerXml
            $blockXml = "<$tagName$attrStr>$innerXml</$tagName>"
            
            # Normalize self-closing tags (remove space before />)
            $blockXml = $blockXml -replace '\s+/>', '/>'
            
            $sw.WriteLine($blockXml)
            $sw.WriteLine() # empty line
        }
        
        $sw.WriteLine("</body>")
        $sw.WriteLine("<back></back>")
        $sw.WriteLine("</text>")
        $sw.WriteLine("</TEI.2>")
        $sw.Close()
    }
    
    # Write TOC file
    $tocPath = Join-Path $destDir "$stem.toc.xml"
    $sw = New-Object System.IO.StreamWriter($tocPath, $false, [System.Text.Encoding]::BigEndianUnicode)
    $sw.NewLine = "`r`n"
    $sw.WriteLine("<?xml version=`"1.0`" encoding=`"UTF-16`"?>")
    $sw.WriteLine("<tree>")
    foreach ($f_info in $fragments) {
        $sw.WriteLine("<tree text=`"$($f_info.title)`" action=`"cscd/$($f_info.filename)`" target=`"text`"/>")
    }
    $sw.WriteLine("</tree>")
    $sw.Close()
    
    $counter++
    if ($counter % 20 -eq 0 -or $counter -eq $totalFiles) {
        Write-Host "Processed $counter of $totalFiles files."
    }
}
