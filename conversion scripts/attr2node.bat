REM attr2node.bat: will apply the attr2node XSLT on all XML files in the current directory.
REM New XML files will be placed in the attr2node directory
for %%V IN (*.xml) DO msxsl "%%V" attr2node.xslt > ./attr2node/%%V