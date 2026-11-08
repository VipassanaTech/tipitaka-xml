@echo off
REM DevaNormalize.bat: normalizes the text of every book in "deva master" after corrections:
REM en dash, double danda, ellipsis, abbreviation sign, and the ZWJ in conjuncts such as
REM kka and nna. Correctors do not need to type these. Double-click it, or run it from
REM this directory. Only the paragraph text is changed; markup and encoding are kept.
"%~dp0DevaNormalize.exe" "%~dp0..\deva master"
pause
