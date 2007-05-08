<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl = "http://www.w3.org/1999/XSL/Transform" version = "1.0" > 


<xsl:template match = "/" > 
<html>
<head>
<title></title>
<link rel="stylesheet" href="tipitaka-mlym.css"/>
</head>
<body>
<xsl:apply-templates select="/*"/>
</body>
</html>
</xsl:template>

<xsl:template match="bodytext">
<p class="bodytext">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="fnote">
<span class="fnote">[
<xsl:apply-templates/>
]</span>
</xsl:template>

<xsl:template match="bold">
<span class="bld">
<xsl:apply-templates/>
</span>
</xsl:template>

<xsl:template match="dot">
<xsl:apply-templates/>
</xsl:template>

<xsl:template match="centre">
<p class="centered">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="book">
<p class="book">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="chapter">
<p class="chapter">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="subhead">
<p class="subhead">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="nikaya">
<p class="nikaya">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="title">
<p class="title">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="gatha1">
<p class="gatha1">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="gatha2">
<p class="gatha2">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="gatha3">
<p class="gatha3">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="gathalast">
<p class="gathalast">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="indent">
<p class="indent">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="unk">
<p class="unk">
<xsl:apply-templates/>
</p>
</xsl:template>

</xsl:stylesheet>