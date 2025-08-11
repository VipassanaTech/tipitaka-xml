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

<xsl:template match='p[@rend="bodytext"]'>
<p class="bodytext">
  <!-- if the n attribute is set, create an HTML anchor for the paragraph in the form pr### -->
  <xsl:if test="@n">
    <a>
      <xsl:attribute name="name">
        <xsl:text>para</xsl:text>
        <xsl:value-of select="@n"/>
      </xsl:attribute>
    </a>
  </xsl:if>
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="hangnum"]'>
<p class="hangnum">
  <!-- if the n attribute is set, create an HTML anchor for the paragraph in the form para### -->
  <xsl:if test="@n">
    <a>
      <xsl:attribute name="name">
        <xsl:text>para</xsl:text>
        <xsl:value-of select="@n"/>
      </xsl:attribute>
    </a>
  </xsl:if>
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="unindented"]'>
<p class="unindented">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="indent"]'>
<p class="indent">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match="note">
<span class="note">[<xsl:apply-templates/>]</span>
</xsl:template>

<xsl:template match='hi[@rend="bold"]'>
<span class="bld"><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match='hi[@rend="paranum"]'>
<span class="paranum"><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match='p[@rend="centre"]'>
<p class="centered">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="subsubhead"]'>
<p class="subsubhead">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='hi[@rend="dot"]'>
<xsl:apply-templates/>
</xsl:template>

<xsl:template match='p[@rend="book"]'>
<p class="book">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="chapter"]'>
<p class="chapter">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="subhead"]'>
<p class="subhead">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="nikaya"]'>
<p class="nikaya">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="title"]'>
<p class="title">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="gatha1"]'>
<p class="gatha1">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="gatha2"]'>
<p class="gatha2">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="gatha3"]'>
<p class="gatha3">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="gathalast"]'>
<p class="gathalast">
<xsl:apply-templates/>
</p>
</xsl:template>

  <xsl:template match="pb">
    <a>
      <xsl:attribute name="name">
        <xsl:value-of select="@ed"/>
        <xsl:value-of select="@n"/>
      </xsl:attribute>
    </a>
  </xsl:template>

</xsl:stylesheet>