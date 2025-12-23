using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PrintMate.Terminal.Services;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для AddProjectSuccessfullView.xaml
    /// </summary>
    public partial class AddProjectSuccessfullView : UserControl
    {

        public static readonly DependencyProperty TimeSpanProperty =
            DependencyProperty.Register(
                nameof(GifDuration),
                typeof(TimeSpan),
                typeof(AddProjectSuccessfullView),
                new PropertyMetadata(TimeSpan.FromSeconds(5)));
        public TimeSpan GifDuration
        {
            get => (TimeSpan)GetValue(TimeSpanProperty);
            set => SetValue(TimeSpanProperty, value);
        }


        public AddProjectSuccessfullView()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Запускает красивую последовательную анимацию всех элементов
        /// </summary>
        public void StartFadeInAnimation()
        {
            // 1. GIF контейнер: bounce scale + fade (0ms задержка)
            AnimateGifContainer();

            // 2. Заголовок: slide-up + fade (задержка 400ms)
            AnimateTitleText();

            // 3. Подзаголовок: slide-up + fade (задержка 600ms)
            AnimateSubtitleText();

            // 4. Кнопка: scale + fade (задержка 800ms)
            AnimateButton();
        }

        /// <summary>
        /// Анимация GIF контейнера: bounce scale + fade
        /// </summary>
        private void AnimateGifContainer()
        {
            var storyboard = new Storyboard();

            // Fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, GifImageBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // Scale bounce (0.3 → 1.0 с bounce)
            var scaleX = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 5 }
            };
            Storyboard.SetTarget(scaleX, GifScaleTransform);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 5 }
            };
            Storyboard.SetTarget(scaleY, GifScaleTransform);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            storyboard.Begin();
        }

        /// <summary>
        /// Анимация заголовка: slide-up + fade
        /// </summary>
        private void AnimateTitleText()
        {
            var storyboard = new Storyboard { BeginTime = TimeSpan.FromMilliseconds(400) };

            // Fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, TitleText);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // Slide up (Y: 30 → 0)
            var slideUp = new DoubleAnimation
            {
                From = 30,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideUp, TitleTranslateTransform);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideUp);

            storyboard.Begin();
        }

        /// <summary>
        /// Анимация подзаголовка: slide-up + fade
        /// </summary>
        private void AnimateSubtitleText()
        {
            var storyboard = new Storyboard { BeginTime = TimeSpan.FromMilliseconds(600) };

            // Fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, SubtitleText);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // Slide up (Y: 20 → 0)
            var slideUp = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideUp, SubtitleTranslateTransform);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideUp);

            storyboard.Begin();
        }

        /// <summary>
        /// Анимация кнопки: scale + fade с легким bounce
        /// </summary>
        private void AnimateButton()
        {
            var storyboard = new Storyboard { BeginTime = TimeSpan.FromMilliseconds(800) };

            // Fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, NextButton);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // Scale X (0.8 → 1.0 с bounce)
            var scaleX = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            Storyboard.SetTarget(scaleX, ButtonScaleTransform);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            // Scale Y (0.8 → 1.0 с bounce)
            var scaleY = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            Storyboard.SetTarget(scaleY, ButtonScaleTransform);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            storyboard.Begin();
        }

        private void NextButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModalService.Instance.Close();
        }
    }
}
