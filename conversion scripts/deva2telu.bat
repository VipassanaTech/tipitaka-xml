REM deva2telu.bat: will run deva2telu.exe on all Deva XML files in the current directory.
REM New Telu XML files will be placed in  the /telu directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2telu.exe "%%V" ../telu