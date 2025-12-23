using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using Prism.Regions;
using ProjectParserTest.Parsers.Shared.Models;
using MessageBox = HandyControl.Controls.MessageBox;

namespace PrintMate.Terminal.Views
{
    public partial class WelcomeView : UserControl
    {
        public static WelcomeView Instance { get; private set; }
        private bool _navigated;
        private bool _firstLoad = true;
        private bool _isAnimatingWelcome = false; // Флаг для предотвращения множественных вызовов

        public WelcomeView()
        {
            InitializeComponent();
            Instance = this;

            // Подписываемся на событие успешного входа
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[WelcomeView] Loaded event");

            // Подписываемся на событие при первой загрузке
            if (DataContext is WelcomeViewModel vm)
            {
                Console.WriteLine($"[WelcomeView] ViewModel found in Loaded: {vm.GetHashCode()}");
                vm.OnLoginSuccess -= OnLoginSuccess; // Отписываемся на всякий случай
                vm.OnLoginSuccess += OnLoginSuccess;
                Console.WriteLine("[WelcomeView] Subscribed to OnLoginSuccess in Loaded");
            }
            else
            {
                Console.WriteLine("[WelcomeView] WARNING: No ViewModel in Loaded!");
            }

            // ВАЖНО: Запускаем анимацию только при первой загрузке
            if (_firstLoad)
            {
                _firstLoad = false;
                Console.WriteLine("[WelcomeView] First load - starting ShowSequentially animation");
                if (Resources["ShowSequentially"] is Storyboard sb)
                {
                    sb.Begin(this);
                }
            }
            else
            {
                Console.WriteLine("[WelcomeView] Not first load - skipping animation");
            }
        }

        /// <summary>
        /// Сбрасывает состояние View и анимаций для повторного использования
        /// </summary>
        public void ResetState()
        {
            Console.WriteLine("[WelcomeView] ResetState called");
            Console.WriteLine($"[WelcomeView] Current DataContext: {DataContext?.GetType().Name ?? "NULL"}");

            // ВАЖНО: Останавливаем все анимации, чтобы они не перезаписывали значения
            ((Storyboard)Resources["HideLoginFormStoryboard"]).Stop(this);
            ((Storyboard)Resources["ShowWelcomeStoryboard"]).Stop(this);
            ((Storyboard)Resources["HideWelcomeStoryboard"]).Stop(this);
            ((Storyboard)Resources["HideUpStoryboard"]).Stop(this);
            ((Storyboard)Resources["ShowSequentially"]).Stop(this);

            // КРИТИЧЕСКИ ВАЖНО: Освобождаем свойства Opacity от контроля анимаций
            // Без этого анимации продолжают контролировать значения даже после Stop()!
            LoginFormContainer.BeginAnimation(UIElement.OpacityProperty, null);
            WelcomeText.BeginAnimation(UIElement.OpacityProperty, null);
            WelcomeTextSub.BeginAnimation(UIElement.OpacityProperty, null);
            Bar.BeginAnimation(UIElement.OpacityProperty, null);
            Icon.BeginAnimation(UIElement.OpacityProperty, null);
            LoginCard.BeginAnimation(UIElement.OpacityProperty, null);
            NextButton.BeginAnimation(UIElement.OpacityProperty, null);
            GlowCircle1.BeginAnimation(UIElement.OpacityProperty, null);
            GlowCircle2.BeginAnimation(UIElement.OpacityProperty, null);
            VersionText.BeginAnimation(UIElement.OpacityProperty, null);
            Title.BeginAnimation(UIElement.OpacityProperty, null);
            SubTitle.BeginAnimation(UIElement.OpacityProperty, null);

            // Сбрасываем флаги
            _navigated = false;
            _firstLoad = false; // Больше не первая загрузка
            _isAnimatingWelcome = false; // Сбрасываем флаг анимации

            // Сбрасываем визуальное состояние элементов формы авторизации
            LoginFormContainer.Opacity = 1;
            WelcomeText.Opacity = 0;
            WelcomeTextSub.Opacity = 0;
            Bar.Opacity = 0;
            WelcomeContainer.Visibility = Visibility.Collapsed;

            // Делаем видимыми элементы формы входа (которые скрыты при первой загрузке)
            Icon.Opacity = 1;
            LoginCard.Opacity = 1;
            NextButton.Opacity = 1;
            NextButton.IsEnabled = true;
            Title.Opacity = 1;
            SubTitle.Opacity = 1;
            //FooterText.Opacity = 1;
            GlowCircle1.Opacity = 0.06;  // Устанавливаем правильные значения как после анимации
            GlowCircle2.Opacity = 0.04;
            VersionText.Opacity = 1;

            // Освобождаем Transform свойства от контроля анимаций
            WelcomeTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            WelcomeTranslateSub.BeginAnimation(TranslateTransform.YProperty, null);
            WelcomeScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            WelcomeScaleSub.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            LoginCardTransform.BeginAnimation(TranslateTransform.YProperty, null);
            IconTransform.BeginAnimation(TranslateTransform.YProperty, null);
            TitleTransform.BeginAnimation(TranslateTransform.YProperty, null);
            SubTitleTransform.BeginAnimation(TranslateTransform.YProperty, null);
            ButtonTransform.BeginAnimation(TranslateTransform.YProperty, null);
            IconScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            IconScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            LoadingSpinner.BeginAnimation(UIElement.OpacityProperty, null);
            LoadingSpinnerTransform.BeginAnimation(TranslateTransform.YProperty, null);

            // Сбрасываем трансформации
            WelcomeTranslate.Y = 0;
            WelcomeTranslateSub.Y = 0;
            WelcomeScale.ScaleY = 1;
            WelcomeScaleSub.ScaleY = 1;
            LoginCardTransform.Y = 0;
            IconTransform.Y = 0;
            TitleTransform.Y = 0;
            SubTitleTransform.Y = 0;
            ButtonTransform.Y = 0;
            IconScale.ScaleX = 1;
            IconScale.ScaleY = 1;
            LoadingSpinner.Opacity = 1;
            LoadingSpinnerTransform.Y = 0;

            Console.WriteLine("[WelcomeView] Visual state reset complete");

            // Используем Dispatcher.BeginInvoke чтобы дождаться обновления DataContext после навигации
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is WelcomeViewModel vm)
                {
                    Console.WriteLine($"[WelcomeView] Resetting ViewModel {vm.GetHashCode()}");
                    vm.Login = string.Empty;
                    vm.Password = string.Empty;

                    // Переподписываемся на событие
                    vm.OnLoginSuccess -= OnLoginSuccess;
                    vm.OnLoginSuccess += OnLoginSuccess;
                    Console.WriteLine("[WelcomeView] Re-subscribed to OnLoginSuccess in ResetState");
                }
                else
                {
                    Console.WriteLine("[WelcomeView] WARNING: DataContext is not WelcomeViewModel after delay!");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine($"DataContextChanged: Old={e.OldValue?.GetType().Name}, New={e.NewValue?.GetType().Name}");

            if (e.OldValue is WelcomeViewModel oldVm)
            {
                oldVm.OnLoginSuccess -= OnLoginSuccess;
            }

            if (e.NewValue is WelcomeViewModel newVm)
            {
                newVm.OnLoginSuccess += OnLoginSuccess;
                Console.WriteLine("Subscribed to OnLoginSuccess in DataContextChanged");
            }
        }

        private async void OnLoginSuccess(string userName)
        {
            if (_isAnimatingWelcome)
            {
                Console.WriteLine("[WelcomeView] Welcome animation already in progress, ignoring");
                return;
            }

            _isAnimatingWelcome = true;
            Console.WriteLine("ON LOGIC SUCCESS");

            // 1. Скрываем форму авторизации
            if (Resources["HideLoginFormStoryboard"] is Storyboard hideLoginSb)
            {
                EventHandler hideLoginCompletedHandler = null;
                hideLoginCompletedHandler = async (s, args) =>
                {
                    hideLoginSb.Completed -= hideLoginCompletedHandler; // Отписываемся сразу

                    // 2. Устанавливаем текст приветствия и показываем его
                    WelcomeText.Text = $"Добро пожаловать, {userName}!";

                    if (Resources["ShowWelcomeStoryboard"] is Storyboard showWelcomeSb)
                    {
                        EventHandler showWelcomeCompletedHandler = null;
                        showWelcomeCompletedHandler = async (s2, args2) =>
                        {
                            showWelcomeSb.Completed -= showWelcomeCompletedHandler; // Отписываемся сразу

                            // 3. Ждём немного и скрываем приветственный текст
                            await System.Threading.Tasks.Task.Delay(1500);

                            if (Resources["HideWelcomeStoryboard"] is Storyboard hideWelcomeSb)
                            {
                                EventHandler hideWelcomeCompletedHandler = null;
                                hideWelcomeCompletedHandler = async (s3, args3) =>
                                {
                                    hideWelcomeSb.Completed -= hideWelcomeCompletedHandler; // Отписываемся сразу

                                    Console.WriteLine("[WelcomeView] CompleteWelcomeAnimationAsync called");

                                    // 4. Загружаем основной контент приложения
                                    if (DataContext is WelcomeViewModel vm)
                                    {
                                        await vm.CompleteWelcomeAnimationAsync();
                                    }

                                    _isAnimatingWelcome = false; // Сбрасываем флаг после завершения
                                };
                                hideWelcomeSb.Completed += hideWelcomeCompletedHandler;
                                hideWelcomeSb.Begin(this);
                            }
                        };
                        showWelcomeSb.Completed += showWelcomeCompletedHandler;
                        showWelcomeSb.Begin(this);
                    }
                };
                hideLoginSb.Completed += hideLoginCompletedHandler;
                hideLoginSb.Begin(this);
            }
        }

        private void NextButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_navigated) return;

            if (Resources["HideUpStoryboard"] is Storyboard sb)
            {
                NextButton.IsEnabled = false;             // защита от двойного клика
                sb.Completed -= HideUpStoryboard_Completed;
                sb.Completed += HideUpStoryboard_Completed; // ВАЖНО: подписаться
                sb.Begin(this);                            // запуск анимации ухода вверх
            }
        }

        private void HideUpStoryboard_Completed(object sender, EventArgs e)
        {
            if (_navigated) return;
            _navigated = true;

            var vm = DataContext;

            // 1) Если кнопке всё-таки зададут Command в XAML — используем его
            if (NextButton.Command is ICommand btnCmd && btnCmd.CanExecute(null))
            {
                btnCmd.Execute(null);
                return;
            }

            // 2) Иначе пытаемся найти в VM свойство NextCommand (рекомендуемый путь)
            var prop = vm?.GetType().GetProperty("NextCommand", BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetValue(vm) is ICommand cmd && cmd.CanExecute(null))
            {
                cmd.Execute(null);
                return;
            }

            // 3) На крайний случай — поддержка поля NextCommand (как у вас было)
            var field = vm?.GetType().GetField("NextCommand", BindingFlags.Public | BindingFlags.Instance);
            if (field?.GetValue(vm) is ICommand fieldCmd && fieldCmd.CanExecute(null))
            {
                fieldCmd.Execute(null);
            }
        }

        private void NextButton_OnTouchDown(object sender, TouchEventArgs e)
        {
            if (DataContext != null && DataContext is WelcomeViewModel vm)
            {
                vm.LoginCommand.Execute(null);
            }
        }

        private void NextButton_OnClick(object sender, RoutedEventArgs e)
        {
            //if (TouchScreenHelper.IsTouchScreenAvailable())
            //{
            //    //MessageBox.Show("RETURN");
            //    return;
            //}
            if (DataContext != null && DataContext is WelcomeViewModel vm)
            {
                vm.LoginCommand.Execute(null);
            }
        }

        private void LoginField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WelcomeViewModel vm)
            {
                vm.ClickLoginCommand?.Execute(null);
            }
        }

        private void LoginField_TouchDown(object sender, TouchEventArgs e)
        {
            if (DataContext is WelcomeViewModel vm)
            {
                vm.ClickLoginCommand?.Execute(null);
            }
        }

        private void PasswordField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WelcomeViewModel vm)
            {
                vm.ClickPasswordCommand?.Execute(null);
            }
        }

        private void PasswordField_TouchDown(object sender, TouchEventArgs e)
        {
            if (DataContext is WelcomeViewModel vm)
            {
                vm.ClickPasswordCommand?.Execute(null);
            }
        }
    }
}