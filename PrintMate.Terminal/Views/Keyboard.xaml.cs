using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.Views.KeyboardComponents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PrintMate.Terminal.Views
{
    public partial class Keyboard : UserControl
    {
        // Флаг для отслеживания создания UI
        private bool _uiCreated = false;
        private KeyboardType _currentKeyboardType = KeyboardType.Full;
        private KeyboardLanguage _currentLanguage = KeyboardLanguage.English;

        public Keyboard()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            // Подписываемся на события клавиатуры
            Loaded += (s, e) =>
            {
                // Устанавливаем фокус на UserControl
                Focusable = true;
                Focus();

                // Запускаем анимацию появления фона
                StartBackdropAnimation();

                // Запускаем анимацию индикатора загрузки
                StartLoadingAnimation();
            };

            // Подписываемся на события физической клавиатуры
            PreviewKeyDown += OnPhysicalKeyDown;
        }

        /// <summary>
        /// Запускает анимацию появления полупрозрачного фона
        /// </summary>
        private void StartBackdropAnimation()
        {
            //if (BackdropOverlay != null)
            //{
            //    var fadeIn = new DoubleAnimation
            //    {
            //        From = 0,
            //        To = 1,
            //        Duration = TimeSpan.FromMilliseconds(300),
            //        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            //    };
            //    BackdropOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            //}
        }

        /// <summary>
        /// Запускает анимацию индикатора загрузки
        /// </summary>
        private void StartLoadingAnimation()
        {
            if (LoadingText != null && LoadingTextSubTitle != null)
            {
                var pulseAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                LoadingText.BeginAnimation(UIElement.OpacityProperty, pulseAnimation);
                LoadingTextSubTitle.BeginAnimation(UIElement.OpacityProperty, pulseAnimation);
            }
        }

        /// <summary>
        /// Останавливает анимацию загрузки и скрывает индикатор
        /// </summary>
        private void HideLoadingIndicator()
        {
            if (LoadingIndicator != null) LoadingIndicator.Visibility = Visibility.Collapsed;
            if (LoadingProgressBar != null) LoadingProgressBar.Visibility = Visibility.Collapsed;
            if (LoadingText != null) LoadingText.Visibility = Visibility.Collapsed;
            if (LoadingTextSubTitle != null) LoadingTextSubTitle.Visibility = Visibility.Collapsed;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is KeyboardViewModel viewModel)
            {
                // Проверяем, нужно ли создавать UI заново или переиспользовать существующий
                // ВАЖНО: делаем это ДО обновления _currentKeyboardType и _currentLanguage
                bool needsRecreate = !_uiCreated ||
                                    _currentKeyboardType != viewModel.KeyboardType ||
                                    _currentLanguage != viewModel.InitialLanguage;

                // Теперь устанавливаем тип клавиатуры для валидации
                _currentKeyboardType = viewModel.KeyboardType;
                _currentLanguage = viewModel.InitialLanguage;

                // Синхронизируем данные
                Title = viewModel.Title ?? "Введите значение";
                Value = viewModel.Value ?? string.Empty;

                // Подписываемся на изменение Value в UI для обновления ViewModel
                ValueChanged -= UpdateViewModelValue;
                ValueChanged += UpdateViewModelValue;

                void UpdateViewModelValue(string value)
                {
                    if (DataContext is KeyboardViewModel vm)
                    {
                        vm.Value = value;
                    }
                }

                if (!needsRecreate)
                {
                    // UI уже создан и подходит - просто показываем
                    HideLoadingIndicator();
                    TextAreaBorder.Visibility = Visibility.Visible;
                    ShowKeyboard(viewModel.KeyboardType);

                    // Валидируем начальное значение для числовой клавиатуры
                    if (viewModel.KeyboardType == KeyboardType.Numpad)
                    {
                        ValidateNumericInput(Value);
                    }
                    return;
                }
                else if (_uiCreated)
                {
                    // UI создан, но не подходит - скрываем старый
                    HideAllKeyboards();
                    TextAreaBorder.Visibility = Visibility.Collapsed;
                }

                // ОТЛОЖЕННОЕ СОЗДАНИЕ UI - дожидаемся окончания анимации появления
                var modalService = ModalService.Instance;
                if (modalService != null)
                {
                    Action<string> handler = null;
                    handler = (modalId) =>
                    {
                        if (modalId == "keyboard_modal")
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                CreateKeyboardUI(viewModel.KeyboardType, viewModel.InitialLanguage);

                                // Запоминаем параметры созданного UI
                                _uiCreated = true;
                                _currentKeyboardType = viewModel.KeyboardType;
                                _currentLanguage = viewModel.InitialLanguage;

                                // Скрываем индикатор загрузки
                                HideLoadingIndicator();
                                TextAreaBorder.Visibility = Visibility.Visible;

                                // Показываем клавиатуру
                                ShowKeyboard(viewModel.KeyboardType);

                                // Валидируем начальное значение для числовой клавиатуры
                                if (viewModel.KeyboardType == KeyboardType.Numpad)
                                {
                                    ValidateNumericInput(Value);
                                }

                                // Восстанавливаем фокус
                                Focusable = true;
                                Focus();
                            }), System.Windows.Threading.DispatcherPriority.Loaded);

                            modalService.OnOpenAnimationFinish -= handler;
                        }
                    };
                    modalService.OnOpenAnimationFinish += handler;
                }
            }
        }

        /// <summary>
        /// Создаёт UI клавиатуры
        /// </summary>
        private void CreateKeyboardUI(KeyboardType type, KeyboardLanguage language)
        {
            if (type == KeyboardType.Full)
            {
                // Подписываемся на события полной клавиатуры
                FullKeyboardControl.KeyPressed += OnKeyPressed;
                FullKeyboardControl.BackspacePressed += OnBackspacePressed;
                FullKeyboardControl.EnterPressed += OnEnterPressed;
                FullKeyboardControl.CancelPressed += OnCancelPressed;
                FullKeyboardControl.SetLanguage(language);
            }
            else
            {
                // Подписываемся на события числовой клавиатуры
                NumpadKeyboardControl.DigitEntered += OnKeyPressed;
                NumpadKeyboardControl.MinusPressed += OnMinusPressed;
            }

            System.Diagnostics.Debug.WriteLine($"[Keyboard] UI created for {type}");
        }

        /// <summary>
        /// Показывает нужную клавиатуру
        /// </summary>
        private void ShowKeyboard(KeyboardType type)
        {
            HideAllKeyboards();

            if (type == KeyboardType.Full)
            {
                FullKeyboardControl.Visibility = Visibility.Visible;
                NumpadButtonsPanel.Visibility = Visibility.Collapsed;
                // Скрываем кнопку Backspace рядом со значением - у полной клавиатуры есть своя
                if (BackspaceButton != null)
                    BackspaceButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                NumpadKeyboardControl.Visibility = Visibility.Visible;
                NumpadButtonsPanel.Visibility = Visibility.Visible;
                // Показываем кнопку Backspace для numpad
                if (BackspaceButton != null)
                    BackspaceButton.Visibility = Visibility.Visible;
            }

            KeyboardContainer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Скрывает все клавиатуры
        /// </summary>
        private void HideAllKeyboards()
        {
            FullKeyboardControl.Visibility = Visibility.Collapsed;
            NumpadKeyboardControl.Visibility = Visibility.Collapsed;
            NumpadButtonsPanel.Visibility = Visibility.Collapsed;
        }

        // События от клавиатурных компонентов
        private void OnKeyPressed(string key)
        {
            Value += key;
        }

        private void OnBackspacePressed()
        {
            if (!string.IsNullOrEmpty(Value))
            {
                Value = Value.Substring(0, Value.Length - 1);
            }
        }

        private void OnEnterPressed()
        {
            EnterButtonOnClick(null, null);
        }

        private void OnCancelPressed()
        {
            CloseButtonOnClick(null, null);
        }

        /// <summary>
        /// Обработчик нажатия минуса - переключает знак числа
        /// </summary>
        private void OnMinusPressed()
        {
            if (string.IsNullOrEmpty(Value))
            {
                Value = "-";
            }
            else if (Value.StartsWith("-"))
            {
                Value = Value.Substring(1);
            }
            else
            {
                Value = "-" + Value;
            }
        }

        /// <summary>
        /// Обработчик кнопки Backspace в области значения
        /// </summary>
        private void OnBackspaceButtonClick(object sender, RoutedEventArgs e)
        {
            OnBackspacePressed();
        }

        // DependencyProperty для Value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(Keyboard),
                new PropertyMetadata(string.Empty, OnValueChanged));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Keyboard keyboard && e.NewValue is string newValue)
            {
                keyboard.ValueChanged?.Invoke(newValue);

                // Валидация для числовой клавиатуры
                if (keyboard._currentKeyboardType == KeyboardType.Numpad)
                {
                    keyboard.ValidateNumericInput(newValue);
                }
            }
        }

        /// <summary>
        /// Проверяет валидность числового ввода и обновляет состояние кнопки "Применить"
        /// </summary>
        private void ValidateNumericInput(string value)
        {
            bool isValid = false;

            if (!string.IsNullOrWhiteSpace(value))
            {
                // Заменяем запятую на точку для парсинга
                string normalizedValue = value.Replace(',', '.');

                // Пробуем распарсить как double
                isValid = double.TryParse(normalizedValue,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double result);

                // Проверка на ведущие нули: 055 - невалидно, 0.55 - валидно, 0 - валидно
                // Также проверяем отрицательные числа: -055 невалидно, -0.55 валидно
                if (isValid && normalizedValue.Length > 1)
                {
                    string checkValue = normalizedValue;

                    // Если число отрицательное, пропускаем минус
                    if (checkValue.StartsWith("-") && checkValue.Length > 2)
                    {
                        checkValue = checkValue.Substring(1);
                    }

                    // Проверяем: начинается с 0 и следующий символ не точка
                    if (checkValue.Length > 1 && checkValue[0] == '0' && checkValue[1] != '.')
                    {
                        isValid = false;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Keyboard] ValidateNumericInput: value='{value}', normalized='{normalizedValue}', isValid={isValid}, result={result}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Keyboard] ValidateNumericInput: value is empty or whitespace");
            }

            // Обновляем состояние кнопки "Применить"
            if (NumEnter != null)
            {
                NumEnter.IsEnabled = isValid;
                System.Diagnostics.Debug.WriteLine($"[Keyboard] NumEnter.IsEnabled = {isValid}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Keyboard] NumEnter is NULL!");
            }

            // Обновляем цвет текста значения (красный если невалидно)
            if (ValueText != null)
            {
                ValueText.Foreground = isValid
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35)); // Red
            }
        }

        public event Action<string> ValueChanged;

        // DependencyProperty для Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(Keyboard),
                new PropertyMetadata("Введите значение"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Обработчики кнопок
        public void EnterButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is KeyboardViewModel viewModel)
            {
                viewModel.IsConfirmed = true;
                ModalService.Instance.Close("keyboard_modal", true);
                SubscribeToModalClosed();
            }
        }

        public void CloseButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is KeyboardViewModel viewModel)
            {
                viewModel.IsConfirmed = false;
                ModalService.Instance.Close("keyboard_modal", false);
                SubscribeToModalClosed();
            }
        }

        private void SubscribeToModalClosed()
        {
            var modalService = ModalService.Instance;
            if (modalService != null)
            {
                Action<string> handler = null;
                handler = (modalId) =>
                {
                    if (modalId == "keyboard_modal")
                    {
                        PrepareForReuse();
                        modalService.OnModalClosed -= handler;
                    }
                };
                modalService.OnModalClosed += handler;
            }
        }

        private void PrepareForReuse()
        {
            if (TextAreaBorder != null)
            {
                TextAreaBorder.Visibility = Visibility.Collapsed;
            }

            if (LoadingIndicator != null)
            {
                LoadingIndicator.Visibility = Visibility.Visible;
            }

            HideAllKeyboards();

            System.Diagnostics.Debug.WriteLine("[Keyboard] PrepareForReuse completed");
        }

        /// <summary>
        /// Обрабатывает ввод с физической клавиатуры
        /// </summary>
        private void OnPhysicalKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Проверяем комбинацию Shift+Alt для переключения языка
            if (_currentKeyboardType == KeyboardType.Full)
            {
                bool isShiftPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
                bool isAltPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0;

                if (isShiftPressed && isAltPressed)
                {
                    // Переключаем язык
                    _currentLanguage = _currentLanguage == KeyboardLanguage.English
                        ? KeyboardLanguage.Russian
                        : KeyboardLanguage.English;

                    FullKeyboardControl.SetLanguage(_currentLanguage);
                    e.Handled = true;
                    return;
                }
            }

            // Обработка специальных клавиш
            switch (e.Key)
            {
                case System.Windows.Input.Key.Back:
                    OnBackspacePressed();
                    e.Handled = true;
                    return;

                case System.Windows.Input.Key.Enter:
                    OnEnterPressed();
                    e.Handled = true;
                    return;

                case System.Windows.Input.Key.Escape:
                    OnCancelPressed();
                    e.Handled = true;
                    return;

                case System.Windows.Input.Key.Space:
                    OnKeyPressed(" ");
                    e.Handled = true;
                    return;
            }

            // Обработка печатных символов
            string inputChar = GetCharFromKey(e.Key);
            if (!string.IsNullOrEmpty(inputChar))
            {
                OnKeyPressed(inputChar);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Преобразует Key в символ с учётом текущей раскладки
        /// </summary>
        private string GetCharFromKey(System.Windows.Input.Key key)
        {
            // Цифры (всегда одинаковые)
            if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
            {
                return ((char)('0' + (key - System.Windows.Input.Key.D0))).ToString();
            }

            // NumPad цифры
            if (key >= System.Windows.Input.Key.NumPad0 && key <= System.Windows.Input.Key.NumPad9)
            {
                return ((char)('0' + (key - System.Windows.Input.Key.NumPad0))).ToString();
            }

            // Точка и запятая с NumPad
            if (key == System.Windows.Input.Key.Decimal)
                return ".";

            // Для полной клавиатуры обрабатываем буквы с учётом раскладки
            if (_currentKeyboardType == KeyboardType.Full)
            {
                return GetCharFromKeyForFullKeyboard(key);
            }

            return null;
        }

        /// <summary>
        /// Получает символ для полной клавиатуры с учётом раскладки
        /// </summary>
        private string GetCharFromKeyForFullKeyboard(System.Windows.Input.Key key)
        {
            bool isShiftPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;

            // Английская раскладка
            if (_currentLanguage == KeyboardLanguage.English)
            {
                // Буквы A-Z
                if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
                {
                    char baseChar = (char)('a' + (key - System.Windows.Input.Key.A));
                    return isShiftPressed ? char.ToUpper(baseChar).ToString() : baseChar.ToString();
                }

                // Специальные символы
                switch (key)
                {
                    case System.Windows.Input.Key.OemMinus: return isShiftPressed ? "_" : "-";
                    case System.Windows.Input.Key.OemPlus: return isShiftPressed ? "+" : "=";
                    case System.Windows.Input.Key.OemOpenBrackets: return isShiftPressed ? "{" : "[";
                    case System.Windows.Input.Key.OemCloseBrackets: return isShiftPressed ? "}" : "]";
                    case System.Windows.Input.Key.OemPipe: return isShiftPressed ? "|" : "\\";
                    case System.Windows.Input.Key.OemSemicolon: return isShiftPressed ? ":" : ";";
                    case System.Windows.Input.Key.OemQuotes: return isShiftPressed ? "\"" : "'";
                    case System.Windows.Input.Key.OemComma: return isShiftPressed ? "<" : ",";
                    case System.Windows.Input.Key.OemPeriod: return isShiftPressed ? ">" : ".";
                    case System.Windows.Input.Key.OemQuestion: return isShiftPressed ? "?" : "/";
                    case System.Windows.Input.Key.OemTilde: return isShiftPressed ? "~" : "`";
                }
            }
            else // Русская раскладка
            {
                // Маппинг для русских букв (ЙЦУКЕН)
                var rusMapping = new Dictionary<System.Windows.Input.Key, string>
                {
                    { System.Windows.Input.Key.Q, isShiftPressed ? "Й" : "й" },
                    { System.Windows.Input.Key.W, isShiftPressed ? "Ц" : "ц" },
                    { System.Windows.Input.Key.E, isShiftPressed ? "У" : "у" },
                    { System.Windows.Input.Key.R, isShiftPressed ? "К" : "к" },
                    { System.Windows.Input.Key.T, isShiftPressed ? "Е" : "е" },
                    { System.Windows.Input.Key.Y, isShiftPressed ? "Н" : "н" },
                    { System.Windows.Input.Key.U, isShiftPressed ? "Г" : "г" },
                    { System.Windows.Input.Key.I, isShiftPressed ? "Ш" : "ш" },
                    { System.Windows.Input.Key.O, isShiftPressed ? "Щ" : "щ" },
                    { System.Windows.Input.Key.P, isShiftPressed ? "З" : "з" },
                    { System.Windows.Input.Key.OemOpenBrackets, isShiftPressed ? "Х" : "х" },
                    { System.Windows.Input.Key.OemCloseBrackets, isShiftPressed ? "Ъ" : "ъ" },
                    { System.Windows.Input.Key.A, isShiftPressed ? "Ф" : "ф" },
                    { System.Windows.Input.Key.S, isShiftPressed ? "Ы" : "ы" },
                    { System.Windows.Input.Key.D, isShiftPressed ? "В" : "в" },
                    { System.Windows.Input.Key.F, isShiftPressed ? "А" : "а" },
                    { System.Windows.Input.Key.G, isShiftPressed ? "П" : "п" },
                    { System.Windows.Input.Key.H, isShiftPressed ? "Р" : "р" },
                    { System.Windows.Input.Key.J, isShiftPressed ? "О" : "о" },
                    { System.Windows.Input.Key.K, isShiftPressed ? "Л" : "л" },
                    { System.Windows.Input.Key.L, isShiftPressed ? "Д" : "д" },
                    { System.Windows.Input.Key.OemSemicolon, isShiftPressed ? "Ж" : "ж" },
                    { System.Windows.Input.Key.OemQuotes, isShiftPressed ? "Э" : "э" },
                    { System.Windows.Input.Key.Z, isShiftPressed ? "Я" : "я" },
                    { System.Windows.Input.Key.X, isShiftPressed ? "Ч" : "ч" },
                    { System.Windows.Input.Key.C, isShiftPressed ? "С" : "с" },
                    { System.Windows.Input.Key.V, isShiftPressed ? "М" : "м" },
                    { System.Windows.Input.Key.B, isShiftPressed ? "И" : "и" },
                    { System.Windows.Input.Key.N, isShiftPressed ? "Т" : "т" },
                    { System.Windows.Input.Key.M, isShiftPressed ? "Ь" : "ь" },
                    { System.Windows.Input.Key.OemComma, isShiftPressed ? "Б" : "б" },
                    { System.Windows.Input.Key.OemPeriod, isShiftPressed ? "Ю" : "ю" },
                    { System.Windows.Input.Key.OemTilde, isShiftPressed ? "Ё" : "ё" }
                };

                if (rusMapping.TryGetValue(key, out var rusChar))
                {
                    return rusChar;
                }

                // Специальные символы для русской раскладки
                switch (key)
                {
                    case System.Windows.Input.Key.OemPipe: return isShiftPressed ? "/" : "\\";
                    case System.Windows.Input.Key.OemQuestion: return ".";
                }
            }

            return null;
        }
    }
}
