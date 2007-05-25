REM deva2latn.bat: will run deva2latn.exe on all Deva XML files in the current directory.
REM New Latn XML files will be placed in  the /latn directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2latn.exe "%%V" ../latn