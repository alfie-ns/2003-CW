@echo off
setlocal EnableDelayedExpansion

:: Git Commit Importance Script
:: - I made this script over the summer holidays for my other git projects: alfie-ns
:: - Windows version adapted from bash original
:: - Uses cmd's set /p for input handling
:: colours for windows console
set "BOLD=[1m"
set "RESET=[0m"

:: main execution starts here
call :selective_add
call :print_bold "Commit importance:"
echo 1. Trivial
echo 2. Minor
echo 3. Moderate
echo 4. Significant
echo 5. Milestone
echo.

call :get_commit_details
if errorlevel 1 goto :error

git commit -m "%commit_message%" || goto :error
echo Changes committed successfully

call :review_changes
if "%push_changes%"=="yes" (
    git push origin main || goto :error
    echo Local repo pushed to remote origin
    call :print_bold "Commit message: %commit_message%"
) else (
    echo Commit created but not pushed. You can:
    echo 1. Push later with 'git push'
    echo 2. Undo the commit with 'git reset HEAD~1'
)

goto :eof

:: function to print bold text
:print_bold
echo %BOLD%%~1%RESET%
goto :eof

:: function for selective file adding
:selective_add
call :print_bold "Unstaged changes:"
git status --porcelain | findstr /R "^[ ?M]" 

:add_loop
set /p "item=Enter file/directory to add, 'all' (or 'done' to finish): "
if "%item%"=="done" goto :eof
if "%item%"=="all" (
    git add .
    echo Added all changes
    goto :eof
)
if exist "%item%" (
    git add "%item%"
    echo Added: %item%
    goto add_loop
) else (
    echo File/directory not found. Please try again.
    goto add_loop
)

:: function to get commit details
:get_commit_details
:importance_loop
set /p "importance=Enter the importance (1-5): "
if "%importance%"=="1" (
    set "importance_text=Trivial"
) else if "%importance%"=="2" (
    set "importance_text=Minor"
) else if "%importance%"=="3" (
    set "importance_text=Moderate"
) else if "%importance%"=="4" (
    set "importance_text=Significant"
) else if "%importance%"=="5" (
    set "importance_text=Milestone"
) else (
    echo Invalid input. Please try again.
    goto importance_loop
)

set /p "custom_message=Enter a custom message for the commit: "
set "commit_message=%importance_text%: %custom_message%"
goto :eof

:: function to review changes
:review_changes
call :print_bold "Files included in this commit:"
git show --stat --oneline HEAD

:review_loop
set /p "answer=Would you like to push these changes? (y/n): "
if /i "%answer%"=="y" (
    set "push_changes=yes"
    goto :eof
) else if /i "%answer%"=="n" (
    set "push_changes=no"
    goto :eof
) else (
    echo Please answer y or n
    goto review_loop
)

:error
echo Error occurred during execution...
exit /b 1

:eof
endlocal
exit /b 0