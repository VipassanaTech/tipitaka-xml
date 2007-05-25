REM deva2knda.bat: will run deva2knda.exe on all Deva XML files in the current directory.
REM New Knda XML files will be placed in  the /knda directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2knda.exe "%%V" ../knda