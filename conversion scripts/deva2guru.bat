REM deva2guru.bat: will run deva2guru.exe on all Deva XML files in the current directory.
REM New Guru XML files will be placed in  the /guru directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2guru.exe "%%V" ../guru