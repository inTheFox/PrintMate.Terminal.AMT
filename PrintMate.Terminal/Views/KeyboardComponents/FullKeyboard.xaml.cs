using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrintMate.Terminal.Views;

namespace PrintMate.Terminal.Views.KeyboardComponents
{
    public partial class FullKeyboard : UserControl
    {
        public event Action<string> KeyPressed;
        public event Action BackspacePressed;
        public event Action EnterPressed;
        public event Action CancelPressed;
        public event Action<KeyboardLanguage> LanguageChanged;

        private bool _isShiftActive = false;
        private KeyboardLanguage _currentLanguage = KeyboardLanguage.Russian;

        public FullKeyboard()
        {
            InitializeComponent();
        }

        public void SetLanguage(KeyboardLanguage language)
        {
            _currentLanguage = language;
            UpdateLayoutVisibility();
            UpdateAllButtonsContent();
        }

        private void UpdateLayoutVisibility()
        {
            if (_currentLanguage == KeyboardLanguage.English)
            {
                EnglishLayout.Visibility = Visibility.Visible;
                RussianLayout.Visibility = Visibility.Collapsed;
            }
            else
            {
                EnglishLayout.Visibility = Visibility.Collapsed;
                RussianLayout.Visibility = Visibility.Visible;
            }
        }

        private void OnKeyClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                var parts = tag.Split(' ');
                var character = _isShiftActive && parts.Length > 1 ? parts[1] : parts[0];
                KeyPressed?.Invoke(character);

                // Отключаем Shift после ввода символа
                if (_isShiftActive)
                {
                    _isShiftActive = false;
                    UpdateShiftButtonAppearance();
                }
            }
        }

        private void OnShiftClick(object sender, RoutedEventArgs e)
        {
            _isShiftActive = !_isShiftActive;
            UpdateShiftButtonAppearance();
            UpdateAllButtonsContent();
        }

        private void UpdateShiftButtonAppearance()
        {
            // Находим кнопку Shift в текущей раскладке
            Button shiftButton = null;
            if (_currentLanguage == KeyboardLanguage.English)
            {
                shiftButton = ShiftButton;
            }
            else
            {
                // Для русской раскладки тоже есть Shift
                shiftButton = FindName("ShiftButton") as Button;
            }

            if (shiftButton != null)
            {
                shiftButton.Background = _isShiftActive
                    ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
                    : new SolidColorBrush(Color.FromRgb(44, 44, 44));
            }
        }

        /// <summary>
        /// Обновляет отображаемый текст на всех кнопках в зависимости от состояния Shift
        /// </summary>
        private void UpdateAllButtonsContent()
        {
            var currentLayout = _currentLanguage == KeyboardLanguage.English ? EnglishLayout : RussianLayout;

            if (currentLayout == null)
                return;

            // Рекурсивно проходим по всем элементам и обновляем кнопки
            UpdateButtonsInPanel(currentLayout);
        }

        private void UpdateButtonsInPanel(DependencyObject parent)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button && button.Tag is string tag)
                {
                    var parts = tag.Split(' ');
                    if (parts.Length > 1)
                    {
                        // Обновляем Content в зависимости от состояния Shift
                        button.Content = _isShiftActive ? parts[1] : parts[0];
                    }
                }

                // Рекурсивно обрабатываем дочерние элементы
                UpdateButtonsInPanel(child);
            }
        }

        private void OnBackspaceClick(object sender, RoutedEventArgs e)
        {
            BackspacePressed?.Invoke();
        }

        private void OnEnterClick(object sender, RoutedEventArgs e)
        {
            EnterPressed?.Invoke();
        }

        private void OnSpaceClick(object sender, RoutedEventArgs e)
        {
            KeyPressed?.Invoke(" ");
        }

        private void OnLangClick(object sender, RoutedEventArgs e)
        {
            _currentLanguage = _currentLanguage == KeyboardLanguage.English
                ? KeyboardLanguage.Russian
                : KeyboardLanguage.English;

            UpdateLayoutVisibility();
            UpdateAllButtonsContent();
            LanguageChanged?.Invoke(_currentLanguage);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            CancelPressed?.Invoke();
        }
    }
}
