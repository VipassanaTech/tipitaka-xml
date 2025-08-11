REM deva2thai.bat: will run deva2thai.exe on all Deva XML files in the current directory.
REM New Thai XML files will be placed in  the /thai directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2thai.exe "%%V" ../thai