REM node2attr.bat: will apply the node2attr XSLT on all XML files in the current directory.
REM New XML files will be placed in the "final" directory
for %%V IN (*.xml) DO msxsl "%%V" node2attr.xslt > ./final/%%V