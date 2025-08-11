REM deva2mlym.bat: will run deva2mlym.exe on all Deva XML files in the current directory.
REM New Mlym XML files will be placed in  the /mlym directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2mlym.exe "%%V" ../mlym