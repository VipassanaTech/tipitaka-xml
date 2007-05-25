REM deva2cyrl.bat: will run deva2cyrl.exe on all Deva XML files in the current directory.
REM New Cyrl XML files will be placed in  the /cyrl directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2cyrl.exe "%%V" ../cyrl