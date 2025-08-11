REM deva2sinh.bat: will run deva2sinh.exe on all Deva XML files in the current directory.
REM New Sinh XML files will be placed in  the /sinh directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2sinh.exe "%%V" ../sinh