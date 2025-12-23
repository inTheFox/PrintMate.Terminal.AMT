using System;
using System.Windows;
using System.Windows.Controls;

namespace PrintMate.Terminal.Views.KeyboardComponents
{
    public partial class NumpadKeyboard : UserControl
    {
        public event Action<string> DigitEntered;
        public event Action BackspacePressed;
        public event Action MinusPressed;

        public NumpadKeyboard()
        {
            InitializeComponent();
        }

        private void OnDigitClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string digit)
            {
                DigitEntered?.Invoke(digit);
            }
        }

        private void OnBackspaceClick(object sender, RoutedEventArgs e)
        {
            BackspacePressed?.Invoke();
        }

        private void OnMinusClick(object sender, RoutedEventArgs e)
        {
            MinusPressed?.Invoke();
        }
    }
}
