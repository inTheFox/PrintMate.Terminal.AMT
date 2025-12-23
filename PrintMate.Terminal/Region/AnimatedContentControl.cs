using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrintMate.Terminal.Region
{
    public class AnimatedContentControl : ContentControl
    {
        private ContentPresenter _contentPresenter;
        private ScaleTransform _scaleTransform;

        static AnimatedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AnimatedContentControl),
                new FrameworkPropertyMetadata(typeof(AnimatedContentControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
            _scaleTransform = GetTemplateChild("PART_ScaleTransform") as ScaleTransform;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (newContent != null && _contentPresenter != null && _scaleTransform != null)
            {
                Console.WriteLine($"Current View: {newContent.GetType().Name}");
                StartAppearAnimation();
            }
        }

        private void StartAppearAnimation()
        {
            // Сбрасываем предыдущие анимации
            _contentPresenter.BeginAnimation(OpacityProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            // Устанавливаем начальное состояние (маленький и прозрачный)
            _contentPresenter.Opacity = 0;
            _scaleTransform.ScaleX = 0.9;
            _scaleTransform.ScaleY = 0.9;

            // Анимация прозрачности
            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Анимация масштаба (из центра)
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.9,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Запускаем анимации
            _contentPresenter.BeginAnimation(OpacityProperty, opacityAnimation);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
    }
}