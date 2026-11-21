using System.Runtime.InteropServices;

namespace KawaiiTasks.UI;

/// <summary>
/// Пытается сделать окно классической консоли Windows квадратным.
/// Современные терминалы (Windows Terminal, терминалы IDE) сами управляют размером окна,
/// поэтому в них метод намеренно ничего не меняет.
///
/// Как это работает: у классической консоли Windows есть обычное окно Win32. Программа получает его
/// дескриптор (HWND) через GetConsoleWindow и меняет размер/стиль системными функциями user32.dll
/// (P/Invoke - вызов функций операционной системы из C#). На Linux и macOS такого окна у программы нет:
/// размером окна управляет эмулятор терминала, а программа лишь узнаёт ширину/высоту в символах.
/// </summary>
public static class ConsoleWindow
{
    private const int TargetWidth = 500;    // размер окна в ПИКСЕЛЯХ, а не в символах
    private const int TargetHeight = 500;

    // Константы Win32 API (значения взяты из документации Windows):
    private const int GwlStyle = -16;                 // "индекс" параметра окна: его стиль
    private const int WsMaximizeBox = 0x00010000;    // флаг стиля: кнопка "развернуть"
    private const int WsSizeBox = 0x00040000;        // флаг стиля: рамка для изменения размера мышью
    private const int SmCxScreen = 0;                // GetSystemMetrics: ширина экрана в пикселях
    private const int SmCyScreen = 1;                // GetSystemMetrics: высота экрана в пикселях
    private const int SwRestore = 9;                 // ShowWindow: вернуть окно из развёрнутого состояния

    private const uint SwpNoZOrder = 0x0004;         // SetWindowPos: не менять порядок окон по глубине
    private const uint SwpNoActivate = 0x0010;       // SetWindowPos: не отбирать фокус у окна
    private const uint SwpFrameChanged = 0x0020;     // SetWindowPos: перерисовать рамку после смены стиля

    /// <summary>
    /// В классической Windows Console Host центрирует окно на экране,
    /// задаёт размер около 500x500 пикселей и отключает изменение размера.
    /// В других терминалах спокойно оставляет размер на усмотрение хоста.
    /// </summary>
    public static void TrySetSquareWindow()
    {
        // Не Windows - у консоли нет окна Win32. Перенаправленный ввод/вывод (файл, канал, тесты) - окна тоже нет.
        if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected || Console.IsInputRedirected)
            return;

        // Windows Terminal и терминалы IDE работают через псевдоконсоль.
        // Из приложения нельзя надёжно менять размер их вкладки или внешнего окна.
        if (IsModernTerminal())
            return;

        try
        {
            IntPtr window = GetConsoleWindow();   // дескриптор окна консоли; 0 = окна нет (например, запуск из IDE)
            if (window == IntPtr.Zero)
                return;

            int screenWidth = GetSystemMetrics(SmCxScreen);
            int screenHeight = GetSystemMetrics(SmCyScreen);
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            // Окно квадратное и не больше экрана; центрируем его по горизонтали и вертикали.
            int targetSize = Math.Min(TargetWidth, TargetHeight);
            int size = Math.Min(targetSize, Math.Min(screenWidth, screenHeight));
            int width = size;
            int height = size;
            int left = Math.Max(0, (screenWidth - size) / 2);
            int top = Math.Max(0, (screenHeight - size) / 2);

            // Сначала восстанавливаем окно, если пользователь открыл его развёрнутым.
            ShowWindow(window, SwRestore);

            // Убираем рамку изменения размера и кнопку разворачивания.
            int style = GetWindowLong(window, GwlStyle);
            if (style != 0)
            {
                int fixedStyle = style & ~(WsSizeBox | WsMaximizeBox);
                SetWindowLong(window, GwlStyle, fixedStyle);
            }

            // Меняем размер после стиля, чтобы 500x500 включали итоговую рамку окна.
            SetWindowPos(
                window,
                IntPtr.Zero,
                left,
                top,
                width,
                height,
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            // Если API недоступен, приложение продолжает работать с размером терминала.
        }
    }

    /// <summary>
    /// Эти переменные окружения выставляют современные терминалы:
    /// WT_SESSION - Windows Terminal, TERM_PROGRAM - VS Code/iTerm и др., ConEmuPID - ConEmu, ANSICON - ANSICON.
    /// Классический conhost их не задаёт.
    /// </summary>
    private static bool IsModernTerminal()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TERM_PROGRAM"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConEmuPID"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANSICON"));
    }

    // ---- Объявления системных функций (P/Invoke). Реализация находится в DLL Windows, не в нашем коде. ----

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr window, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
