@echo off
rem ---- Запуск Kawaii Tasks в Windows ----
rem @echo off - не печатать сами команды этого файла в окно.
setlocal
rem chcp 65001 - переключить кодовую страницу консоли на UTF-8, чтобы русские буквы и значки выводились правильно.
rem >nul - спрятать сообщение 'Active code page: 65001'.
chcp 65001 >nul
rem %~dp0 - папка, где лежит этот .bat; cd /d переходит туда (в том числе на другой диск).
cd /d "%~dp0"
dotnet run --project "%~dp0KawaiiTasks.csproj"
echo.
rem pause - не закрывать окно сразу после выхода из программы, чтобы можно было прочитать сообщения об ошибках.
pause
endlocal
