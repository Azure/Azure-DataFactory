@echo off

echo Installing PostgreSQL ODBC drivers...

msiexec /i psqlodbc_x64.msi /qn /lv %CUSTOM_SETUP_SCRIPT_LOG_DIR%\psqx64l.LOG

msiexec /i psqlodbc_x86.msi /qn /lv %CUSTOM_SETUP_SCRIPT_LOG_DIR%\psqlx86.LOG

echo Installation completed