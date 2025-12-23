using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PrintMate.Terminal.Views.ComponentsViews
{
    public partial class LogicIndicator : UserControl
    {
        public const string EnabledSrc = "/images/indicator_green_32.png";
        public const string DisabledSrc = "/images/indicator_red_32.png";

        private readonly BitmapImage activeBitmapImage;
        private readonly BitmapImage unActiveBitmapImage;

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(LogicIndicator),
                new PropertyMetadata("Title"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(bool),
                typeof(LogicIndicator),
                new PropertyMetadata(false, ValueChanged));

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogicIndicator control && e.NewValue is bool newValue)
            {
                control.UpdateImageSource(newValue);
            }
        }

        private void UpdateImageSource(bool isActive)
        {
            Image.Source = isActive ? activeBitmapImage : unActiveBitmapImage;
        }

        public bool Value
        {
            get => (bool)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public LogicIndicator()
        {
            // Инициализируем изображения ДО InitializeComponent
            activeBitmapImage = CreateBitmapImage(EnabledSrc);
            unActiveBitmapImage = CreateBitmapImage(DisabledSrc);

            InitializeComponent();
            UpdateImageSource(Value); // Устанавливаем начальное состояние
        }

        private static BitmapImage CreateBitmapImage(string relativePath)
        {
            // relativePath должен быть вида "/images/xxx.png"
            var uri = new Uri($"pack://application:,,,{relativePath}");
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}