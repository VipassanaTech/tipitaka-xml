REM deva2mymr.bat: will run deva2mymr.exe on all Deva XML files in the current directory.
REM New Mymr XML files will be placed in  the /mymr directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2mymr.exe "%%V" ../mymr