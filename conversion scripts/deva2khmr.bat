REM deva2khmr.bat: will run deva2khmr.exe on all Deva XML files in the current directory.
REM New Khmr XML files will be placed in  the /khmr directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2khmr.exe "%%V" ../khmr