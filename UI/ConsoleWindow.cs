// Подключаем пространство имён для работы с P/Invoke — механизмом вызова функций Windows из C#.
using System.Runtime.InteropServices;

// Объявляем пространство имён пользовательского интерфейса проекта.
namespace KawaiiTasks.UI;

/// <summary>
/// Пытается сделать окно классической консоли Windows квадратным.
/// Современные терминалы (Windows Terminal, терминалы IDE) сами управляют размером окна,
/// поэтому в них метод намеренно ничего не меняет.
///
/// Как это работает: у классической консоли Windows есть обычное окно Win32. Программа получает его
/// дескриптор (HWND) через GetConsoleWindow и меняет размер/стиль системными функциями user32.dll
/// (P/Invoke — вызов функций операционной системы из C#). На Linux и macOS такого окна у программы нет:
/// размером окна управляет эмулятор терминала, а программа лишь узнаёт ширину/высоту в символах.
/// </summary>
public static class ConsoleWindow
{
    // Желаемая ширина окна в пикселях.
    // Это именно физические пиксели экрана, а не количество символов консоли.
    private const int TargetWidth = 500;

    // Желаемая высота окна в пикселях.
    private const int TargetHeight = 500;

    // Константы Win32 API. Они определяют параметры окна и способы взаимодействия с ним.

    // GWL_STYLE (-16) — индекс свойства, содержащего основные стили окна.
    private const int GwlStyle = -16;

    // WS_MAXIMIZEBOX — флаг наличия кнопки разворачивания окна на весь экран.
    private const int WsMaximizeBox = 0x00010000;

    // WS_SIZEBOX — флаг наличия рамки, позволяющей изменять размер окна мышью.
    private const int WsSizeBox = 0x00040000;

    // SM_CXSCREEN — параметр GetSystemMetrics, возвращающий ширину основного экрана в пикселях.
    private const int SmCxScreen = 0;

    // SM_CYSCREEN — параметр GetSystemMetrics, возвращающий высоту основного экрана в пикселях.
    private const int SmCyScreen = 1;

    // SW_RESTORE — команда ShowWindow, восстанавливающая окно из свёрнутого или развёрнутого состояния.
    private const int SwRestore = 9;

    // Флаги SetWindowPos определяют дополнительные условия изменения положения и размера окна.

    // SWP_NOZORDER — не менять порядок расположения окна относительно остальных окон.
    private const uint SwpNoZOrder = 0x0004;

    // SWP_NOACTIVATE — не активировать окно и не перехватывать у пользователя фокус ввода.
    private const uint SwpNoActivate = 0x0010;

    // SWP_FRAMECHANGED — сообщить Windows, что стили рамки изменились, и её необходимо обновить.
    private const uint SwpFrameChanged = 0x0020;

    /// <summary>
    /// В классической Windows Console Host центрирует окно на экране,
    /// задаёт размер около 500x500 пикселей и отключает изменение размера.
    /// В других терминалах спокойно оставляет размер на усмотрение хоста.
    /// </summary>
    public static void TrySetSquareWindow()
    {
        // Проверяем, что приложение запущено в Windows.
        // Console.IsOutputRedirected определяет, перенаправлен ли вывод, например, в файл или канал.
        // Console.IsInputRedirected проверяет перенаправление ввода.
        // В таких случаях обычного интерактивного окна консоли может не быть, поэтому выходим.
        if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected || Console.IsInputRedirected)
            return;

        // Windows Terminal и терминалы IDE используют псевдоконсоль.
        // Приложение не может надёжно управлять размером их вкладок и внешних окон через Win32 API.
        // Если обнаружен современный терминал, оставляем управление размером ему.
        if (IsModernTerminal())
            return;

        try
        {
            // Получаем дескриптор окна классической консоли Windows.
            // IntPtr — тип для хранения указателей и системных дескрипторов.
            // GetConsoleWindow возвращает IntPtr.Zero, если окно отсутствует.
            IntPtr window = GetConsoleWindow();

            // Если дескриптор равен нулю, значит, окно консоли получить не удалось.
            // В этом случае дальнейшие операции с окном выполнять нельзя.
            if (window == IntPtr.Zero)
                return;

            // Получаем ширину основного экрана в пикселях через системную функцию Windows.
            int screenWidth = GetSystemMetrics(SmCxScreen);

            // Получаем высоту основного экрана в пикселях.
            int screenHeight = GetSystemMetrics(SmCyScreen);

            // Проверяем корректность полученных размеров.
            // Нулевые или отрицательные значения не позволяют вычислить положение окна.
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            // Определяем целевой размер окна.
            // Math.Min выбирает меньшее из двух значений, поэтому окно будет квадратным.
            int targetSize = Math.Min(TargetWidth, TargetHeight);

            // Ограничиваем размер окна размерами экрана.
            // Это необходимо, чтобы окно не выходило за пределы доступной области.
            int size = Math.Min(targetSize, Math.Min(screenWidth, screenHeight));

            // Для квадратного окна ширина и высота должны совпадать.
            int width = size;
            int height = size;

            // Рассчитываем координату левого края окна.
            // Вычитание размера окна из ширины экрана и деление результата на два
            // позволяют расположить окно по горизонтальному центру.
            // Math.Max не допускает отрицательных координат.
            int left = Math.Max(0, (screenWidth - size) / 2);

            // Аналогично рассчитываем координату верхнего края окна,
            // чтобы расположить его по вертикальному центру экрана.
            int top = Math.Max(0, (screenHeight - size) / 2);

            // Восстанавливаем окно, если оно было свёрнуто или развёрнуто на весь экран.
            // Это позволяет затем задать ему собственные размеры и положение.
            ShowWindow(window, SwRestore);

            // Получаем текущие стили окна.
            // Они определяют его внешний вид и доступные пользователю действия.
            int style = GetWindowLong(window, GwlStyle);

            // Если значение стиля не равно нулю, выполняем изменение.
            if (style != 0)
            {
                // Операция побитового исключающего ИЛИ с инверсией маски
                // позволяет сбросить указанные флаги, не затрагивая остальные.
                // Убираем WS_SIZEBOX и WS_MAXIMIZEBOX:
                // пользователь больше не сможет менять размер окна мышью и разворачивать его кнопкой.
                int fixedStyle = style & ~(WsSizeBox | WsMaximizeBox);

                // Применяем обновлённый набор стилей к окну.
                SetWindowLong(window, GwlStyle, fixedStyle);
            }

            // Изменяем положение и размер окна.
            // Метод SetWindowPos принимает дескриптор, координаты, размеры и дополнительные флаги.
            SetWindowPos(
                window,                         // Дескриптор окна, которое необходимо изменить.
                IntPtr.Zero,                    // Не задаём новое положение окна в Z-порядке.
                left,                           // Координата левого края окна.
                top,                            // Координата верхнего края окна.
                width,                          // Новая ширина окна в пикселях.
                height,                         // Новая высота окна в пикселях.
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged // Объединяем флаги побитовым ИЛИ.
            );
        }
        // Перехватываем ошибки, связанные с отсутствием системной библиотеки,
        // недоступностью нужной функции или несовместимостью формата библиотеки.
        // Фильтр when позволяет обрабатывать только перечисленные типы исключений.
        catch (Exception ex) when (
            ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            // Если системный API недоступен, приложение продолжает работать.
            // Размер окна остаётся под управлением терминала.
        }
    }

    /// <summary>
    /// Эти переменные окружения выставляют современные терминалы:
    /// WT_SESSION — Windows Terminal, TERM_PROGRAM — VS Code/iTerm и др.,
    /// ConEmuPID — ConEmu, ANSICON — ANSICON.
    /// Классический conhost их не задаёт.
    /// </summary>
    private static bool IsModernTerminal()
    {
        // Environment.GetEnvironmentVariable получает значение переменной окружения процесса.
        // string.IsNullOrWhiteSpace проверяет, что значение не равно null,
        // не является пустой строкой и не состоит только из пробелов.
        //
        // Оператор || означает логическое ИЛИ:
        // если хотя бы одна переменная обнаружена, считаем терминал современным.
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TERM_PROGRAM"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConEmuPID"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANSICON"));
    }

    // ---- Объявления системных функций (P/Invoke). Реализация находится в DLL Windows, не в нашем коде. ----

    // DllImport связывает объявление метода C# с функцией из системной библиотеки Windows.
    // kernel32.dll содержит функции для взаимодействия с ядром и консолью Windows.
    // SetLastError = true позволяет сохранять системную ошибку для последующей диагностики.
    [DllImport("kernel32.dll", SetLastError = true)]

    // Получает дескриптор окна, связанного с текущей консолью.
    // extern означает, что реализация метода находится во внешней системной библиотеке.
    private static extern IntPtr GetConsoleWindow();

    // user32.dll содержит функции управления окнами и пользовательским интерфейсом Windows.
    [DllImport("user32.dll", SetLastError = true)]

    // Получает системные параметры дисплея.
    // index определяет, какой именно параметр требуется получить:
    // в нашем случае ширину или высоту экрана.
    private static extern int GetSystemMetrics(int index);

    // Указываем библиотеку user32.dll и имя системной функции ShowWindow.
    [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]

    // Изменяет состояние окна: например, восстанавливает его после сворачивания.
    // window — дескриптор окна, command — команда изменения состояния.
    private static extern bool ShowWindow(IntPtr window, int command);

    // Указываем точное имя функции GetWindowLongW из user32.dll.
    // Суффикс W обозначает вариант функции, использующий Unicode.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]

    // Получает значение одного из параметров окна.
    // window — дескриптор окна, index — индекс нужного свойства.
    // Возвращаемое значение содержит текущий набор стилей окна.
    private static extern int GetWindowLong(IntPtr window, int index);

    // Указываем функцию SetWindowLongW, которая изменяет параметры окна.
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]

    // Устанавливает новое значение свойства окна.
    // value содержит обновлённый набор стилей.
    private static extern int SetWindowLong(IntPtr window, int index, int value);

    // Объявляем функцию изменения размера и положения окна.
    [DllImport("user32.dll", SetLastError = true)]

    // Устанавливает новые координаты и размеры указанного окна.
    private static extern bool SetWindowPos(
        IntPtr window,       // Дескриптор окна, которое нужно переместить или изменить.
        IntPtr insertAfter,  // Дескриптор окна, относительно которого задаётся Z-порядок.
        int x,               // Новая координата левого края окна.
        int y,               // Новая координата верхнего края окна.
        int width,           // Новая ширина окна.
        int height,          // Новая высота окна.
        uint flags);         // Дополнительные флаги изменения положения и отображения окна.
}