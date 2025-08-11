REM deva2tibt.bat: will run deva2tibt.exe on all Deva XML files in the current directory.
REM New Tibt XML files will be placed in  the /tibt directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2tibt.exe "%%V" ../tibt