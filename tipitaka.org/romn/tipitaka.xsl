<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl = "http://www.w3.org/1999/XSL/Transform" version = "1.0" > 


<xsl:template match = "/" > 
<html>
<head>
<title></title>
<link rel="stylesheet" href="tipitaka-dev.css"/>
</head>
<body>
<xsl:apply-templates select="/*"/>
</body>
</html>
</xsl:template>

<xsl:template match="CENTRE">
<p class="centered">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="BOOK">
<p class="book">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="CHAPTER">
<p class="chapter">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="SUBHEAD">
<p class="subhead">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="NIKAYA">
<p class="nikaya">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="TITLE">
<p class="title">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="REF">
<span class="variant">
<xsl:value-of select="."/>
</span>
</xsl:template>

<xsl:template match="FIRST">
<p class="first">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="LAST">
<p class="last">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="SECOND">
<p class="second">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="THIRD">
<p class="third">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="INDENT">
<p class="indent">
<xsl:value-of select="."/>
</p>
</xsl:template>

<xsl:template match="UNK">
<p class="unk">
<xsl:value-of select="."/>
</p>
</xsl:template>

</xsl:stylesheet>