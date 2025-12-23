using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.Views;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace PrintMate.Terminal.Services
{
    public class KeyboardService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        public static KeyboardService Instance { get; private set; }

        private readonly ModalService _modalService;
        private bool _isShowing = false; // Флаг для предотвращения множественных открытий

        public KeyboardService(ModalService modalService)
        {
            _modalService = modalService;
            Instance = this;
        }

        /// <summary>
        /// Показать клавиатуру и дождаться ввода (асинхронная версия)
        /// </summary>
        /// <param name="keyboardType">Тип клавиатуры (Full или Numpad)</param>
        /// <param name="title">Заголовок</param>
        /// <param name="value">Начальное значение</param>
        /// <returns>Введённое значение или null если отменено</returns>
        public async Task<string> ShowAsync(KeyboardType keyboardType, string title, string value)
        {
            System.Diagnostics.Debug.WriteLine($"[KeyboardService] ShowAsync called: type={keyboardType}, title={title}");

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return await Application.Current.Dispatcher.InvokeAsync(() => ShowAsync(keyboardType, title, value)).Task.Unwrap();
            }

            // Определяем начальную раскладку для Full клавиатуры
            var initialLanguage = KeyboardLanguage.English;
            if (keyboardType == KeyboardType.Full)
            {
                var layout = GetKeyboardLayout(0);
                var langId = layout.ToInt32() & 0xFFFF;
                var culture = new System.Globalization.CultureInfo(langId);

                if (culture.Name.Contains("ru"))
                {
                    initialLanguage = KeyboardLanguage.Russian;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[KeyboardService] Initial language: {initialLanguage}");

            // Параметры для инициализации ViewModel
            var options = new System.Collections.Generic.Dictionary<string, object>
            {
                { "KeyboardType", keyboardType },
                { "Title", title },
                { "Value", value ?? string.Empty },
                { "InitialLanguage", initialLanguage }
            };

            System.Diagnostics.Debug.WriteLine($"[KeyboardService] Calling ModalService.ShowAsync...");

            // Показываем клавиатуру через ModalService
            var result = await _modalService.ShowAsync<Keyboard, KeyboardViewModel>(
                modalId: "keyboard_modal",
                options: options,
                showOverlay: true,
                closeOnBackgroundClick: false
            );

            System.Diagnostics.Debug.WriteLine($"[KeyboardService] Modal closed. IsSuccess={result.IsSuccess}, IsConfirmed={result.Result?.IsConfirmed}");

            // Если пользователь подтвердил ввод - возвращаем значение
            if (result.IsSuccess && result.Result.IsConfirmed)
            {
                return result.Result.Value;
            }

            return null;
        }

        /// <summary>
        /// Показать клавиатуру и дождаться ввода (синхронная версия для обратной совместимости)
        /// </summary>
        /// <param name="keyboardType">Тип клавиатуры (Full или Numpad)</param>
        /// <param name="title">Заголовок</param>
        /// <param name="value">Начальное значение</param>
        /// <returns>Введённое значение или null если отменено</returns>
        public string Show(KeyboardType keyboardType, string title, string value)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[KeyboardService] Show called (SYNC): type={keyboardType}, title={title}");

                // Проверяем, не открыта ли уже клавиатура
                if (_isShowing)
                {
                    System.Diagnostics.Debug.WriteLine($"[KeyboardService] Keyboard is already showing, ignoring duplicate call");
                    return value; // Возвращаем текущее значение без изменений
                }

                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    System.Diagnostics.Debug.WriteLine($"[KeyboardService] Not on dispatcher thread, invoking...");
                    return Application.Current.Dispatcher.Invoke(() => Show(keyboardType, title, value));
                }

                _isShowing = true;
                System.Diagnostics.Debug.WriteLine($"[KeyboardService] On dispatcher thread, calling ShowAsync...");

                // Используем вложенный message loop для ожидания без deadlock
                string result = null;
                var frame = new System.Windows.Threading.DispatcherFrame();

                var task = ShowAsync(keyboardType, title, value);
                task.ContinueWith(t =>
                {
                    result = t.Result;
                    frame.Continue = false; // Выходим из вложенного message loop
                    _isShowing = false; // Сбрасываем флаг
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());

                // Запускаем вложенный message loop - это позволяет обрабатывать события UI
                System.Windows.Threading.Dispatcher.PushFrame(frame);

                System.Diagnostics.Debug.WriteLine($"[KeyboardService] ShowAsync completed, result={result}");
                return result;
            }
            catch (Exception ex)
            {
                _isShowing = false; // Сбрасываем флаг при ошибке
                System.Diagnostics.Debug.WriteLine($"[KeyboardService] EXCEPTION: {ex}");
                MessageBox.Show($"Ошибка KeyboardService: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Закрыть клавиатуру
        /// </summary>
        public void Close()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _modalService.Close("keyboard_modal", false);
            });
        }
    }
}
