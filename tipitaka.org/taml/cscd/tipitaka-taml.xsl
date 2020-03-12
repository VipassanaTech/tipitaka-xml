<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl = "http://www.w3.org/1999/XSL/Transform" version = "1.0" > 

<xsl:template match = "/" > 
<html>
<head>
<title></title>
<script>
function setHitsVisibility(isVisible)
{ 
  var c = getStyleClass('hit');
  if (isVisible)
  {
    c.style.backgroundColor = 'blue';
    c.style.color = 'white';
  }
  else
  {
    c.style.backgroundColor = 'white';
    c.style.color = 'black';
  }
}

function setFootnotesVisibility(isVisible)
{
  getStyleClass('note').style.display = (isVisible ? 'inline' : 'none');
}

function getStyleClass (className) {
	for (var s = 0; s &lt; document.styleSheets.length; s++)
	{
		if(document.styleSheets[s].rules)
		{
			for (var r = 0; r &lt; document.styleSheets[s].rules.length; r++)
			{
				if (document.styleSheets[s].rules[r].selectorText == '.' + className)
				{
					return document.styleSheets[s].rules[r];
				}
			}
		}
		else if(document.styleSheets[s].cssRules)
		{
			for (var r = 0; r &lt; document.styleSheets[s].cssRules.length; r++)
			{
				if (document.styleSheets[s].cssRules[r].selectorText == '.' + className)
					return document.styleSheets[s].cssRules[r];
			}
		}
	}
	
	return null;
}
</script>
<style>
body { 
  font-family: Latha;
  background: white;
}

span {}
.note {color: blue}
.bld {font-weight: bold; }
.paranum {font-weight: bold; }
.hit {background-color: blue; color: white; }
.context {background-color: green; color: white; }

p {
  border-top: 0in; border-bottom: 0in;
  padding-top: 0in; padding-bottom: 0in;
  margin-top: 0in; margin-bottom: 0.5cm;
}

.indent { font-size: 12pt; text-indent: 2em; margin-left: 3em;}

.bodytext { font-size: 12pt; text-indent: 2em;}

.hangnum { font-size: 12pt; margin-bottom: -14.4pt; text-indent: 2em;}

/* Namo tassa, and nitthita -- no unique structural distinction */
.centered { font-size: 12pt; text-align:center;}

.unindented { font-size: 12pt;}

.book { font-size: 21pt; text-align:center; font-weight: bold;}

.chapter { font-size: 18pt; text-align:center; font-weight: bold;}

.nikaya { font-size: 24pt; text-align:center; font-weight: bold;}

.title { font-size: 12pt; text-align:center; font-weight: bold;}

.subhead { font-size: 12pt; text-align:center; font-weight: bold;}

.subsubhead { font-size: 12pt; text-align:center; font-weight: bold;}

/* Gatha line 1 */
.gatha1 { font-size: 12pt; margin-bottom: 0em; margin-left: 4em;}

/* Gatha line 2 */
.gatha2 { font-size: 12pt; margin-bottom: 0em; margin-left: 4em;}

/* Gatha line 3 */
.gatha3 { font-size: 12pt; margin-bottom: 0em; margin-left: 4em;}

/* Gatha last line */
.gathalast { font-size: 12pt; margin-bottom: 0.5cm; margin-left: 4em;}
</style>
</head>
<body>
<xsl:apply-templates select="/*"/>
</body>
</html>
</xsl:template>

<xsl:template match='div'>
  <!-- if the id attribute is set, create an HTML anchor for the div -->
  <xsl:if test="@id">
    <a>
      <xsl:attribute name="name">
        <xsl:value-of select="@id"/>
      </xsl:attribute>
    </a>
  </xsl:if>
<xsl:apply-templates/>
</xsl:template>

<xsl:template match='p[@rend="bodytext"]'>
<p class="bodytext">
  <!-- if the n attribute is set, create an HTML anchor for the paragraph in the form para### -->
  <xsl:if test="@n">
    <a>
      <xsl:attribute name="name">
        <xsl:text>para</xsl:text>
        <xsl:value-of select="@n"/>
      </xsl:attribute>
    </a>
    <xsl:if test='ancestor::div[@type="book"]/@id'>
      <a>
        <xsl:attribute name="name">
          <xsl:text>para</xsl:text>
          <xsl:value-of select="@n"/>
          <xsl:text>_</xsl:text>
          <xsl:value-of select='ancestor::div[@type="book"]/@id'/>
        </xsl:attribute>
      </a>
    </xsl:if>
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
    <xsl:if test='ancestor::div[@type="book"]/@id'>
      <a>
        <xsl:attribute name="name">
          <xsl:text>para</xsl:text>
          <xsl:value-of select="@n"/>
          <xsl:text>_</xsl:text>
          <xsl:value-of select='ancestor::div[@type="book"]/@id'/>
        </xsl:attribute>
      </a>
    </xsl:if>
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

<xsl:template match='hi[@rend="hit"]'>
<span class="hit"><xsl:attribute name="id"><xsl:value-of select="@id"/></xsl:attribute><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match='hi[@rend="context"]'>
<span class="context"><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match='hi[@rend="paranum"]'>
<span class="paranum"><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match='p[@rend="centre"]|trailer[@rend="centre"]'>
<p class="centered">
  <!-- if the n attribute is set, create an HTML anchor for the paragraph in the form pr### -->
  <xsl:if test="@n">
    <a>
      <xsl:attribute name="name">
        <xsl:text>para</xsl:text>
        <xsl:value-of select="@n"/>
      </xsl:attribute>
    </a>
    <xsl:if test='ancestor::div[@type="book"]/@id'>
      <a>
        <xsl:attribute name="name">
          <xsl:text>para</xsl:text>
          <xsl:value-of select="@n"/>
          <xsl:text>_</xsl:text>
          <xsl:value-of select='ancestor::div[@type="book"]/@id'/>
        </xsl:attribute>
      </a>
    </xsl:if>
  </xsl:if>
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="subsubhead"]|head[@rend="subsubhead"]'>
<p class="subsubhead">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='hi[@rend="dot"]'>
<xsl:apply-templates/>
</xsl:template>

<xsl:template match='p[@rend="book"]|head[@rend="book"]'>
<p class="book">
<xsl:apply-templates/>
</p>
</xsl:template>

<xsl:template match='p[@rend="chapter"]|head[@rend="chapter"]'>
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

<xsl:template match='p[@rend="title"]|head[@rend="title"]'>
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
<xsl:value-of select="@ed"/><xsl:value-of select="@n"/>
</xsl:attribute>
</a>
</xsl:template>

</xsl:stylesheet>