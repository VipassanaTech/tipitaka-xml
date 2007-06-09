<xsl:stylesheet version = '1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
<xsl:template match="tree"> 
<xsl:choose >
<xsl:when test="@action!=''">
<tree><xsl:attribute name="text"><xsl:value-of select="."/></xsl:attribute><xsl:attribute name="action"><xsl:value-of select="@action"/></xsl:attribute><xsl:attribute name="target"><xsl:value-of select="@target"/></xsl:attribute></tree> 
</xsl:when>
<xsl:otherwise>
<tree>
<xsl:apply-templates/>
</tree>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
</xsl:stylesheet>