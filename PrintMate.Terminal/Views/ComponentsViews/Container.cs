using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrintMate.Terminal.Views.ComponentsViews
{
    public class Container : HeaderedItemsControl
    {
        private ScaleTransform _scaleTransform;
        private TranslateTransform _translateTransform;
        private RotateTransform _rotateTransform;
        private Border _contentBorder;

        static Container()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Container),
                new FrameworkPropertyMetadata(typeof(Container)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _scaleTransform = GetTemplateChild("PART_ScaleTransform") as ScaleTransform;
            _translateTransform = GetTemplateChild("PART_TranslateTransform") as TranslateTransform;
            _rotateTransform = GetTemplateChild("PART_RotateTransform") as RotateTransform;
            _contentBorder = GetTemplateChild("PART_ContentBorder") as Border;

            if (_scaleTransform != null && _translateTransform != null &&
                _rotateTransform != null && _contentBorder != null)
            {
                Loaded += Container_Loaded;
            }
        }

        private void Container_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Container_Loaded;
            StartAppearAnimation();
        }

        private void StartAppearAnimation()
        {
            // Сбрасываем предыдущие анимации
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            _contentBorder.BeginAnimation(OpacityProperty, null);

            // Устанавливаем начальное состояние
            _scaleTransform.ScaleX = 0.7;
            _scaleTransform.ScaleY = 0.7;
            _translateTransform.Y = -30;
            _rotateTransform.Angle = -3;
            _contentBorder.Opacity = 0;

            // Анимация масштаба с bounce эффектом
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
            };

            // Анимация сдвига по Y (сверху вниз)
            var translateYAnimation = new DoubleAnimation
            {
                From = -30,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 }
            };

            // Анимация вращения (возвращение к 0)
            var rotateAnimation = new DoubleAnimation
            {
                From = -3,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 }
            };

            // Анимация прозрачности
            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // Запускаем анимации одновременно
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
            _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            _contentBorder.BeginAnimation(OpacityProperty, opacityAnimation);
        }


        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(Container),
                new PropertyMetadata("Header"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }



        public static readonly DependencyProperty TitleSizeProperty =
            DependencyProperty.Register(
                nameof(TitleSize),
                typeof(int),
                typeof(Container),
                new PropertyMetadata(15));

        public int TitleSize
        {
            get => (int)GetValue(TitleSizeProperty);
            set => SetValue(TitleSizeProperty, value);
        }
    }
}