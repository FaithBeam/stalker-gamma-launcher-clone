SET script_dir=%~dp0
powershell.exe -noprofile -executionpolicy bypass -file %script_dir%build.ps1
