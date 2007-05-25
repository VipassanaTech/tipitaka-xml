REM deva2beng.bat: will run deva2beng.exe on all Deva XML files in the current directory.
REM New Beng XML files will be placed in  the /beng directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2beng.exe "%%V" ../beng