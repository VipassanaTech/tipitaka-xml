REM deva2gujr.bat: will run deva2gujr.exe on all Deva XML files in the current directory.
REM New Gujr XML files will be placed in  the /gujr directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2gujr.exe "%%V" ../gujr