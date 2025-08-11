REM pitaka2xml.bat: will run pitaka2xml.exe -split on all text files in the current directory
for %%V IN (*.txt) DO pitaka2xml.exe -split "%%V"