REM deva2taml.bat: will run deva2taml.exe on all Deva XML files in the current directory.
REM New Taml XML files will be placed in the /taml directory which is at the same level as the /deva directory
for %%V IN (*.xml) DO deva2taml.exe "%%V" ../taml