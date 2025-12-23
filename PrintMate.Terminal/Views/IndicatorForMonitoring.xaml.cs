using Newtonsoft.Json;
using Opc2Lib;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using Prism.Events;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для IndicatorForMonitoringViewModel.xaml
    /// </summary>
    public partial class IndicatorForMonitoring : UserControl
    {
        public const string EnabledSrc = "/images/indicator_green_32.png";
        public const string DisabledSrc = "/images/indicator_red_32.png";

        private BitmapImage activeBitmapImage = new BitmapImage(new Uri(EnabledSrc, UriKind.Relative));
        private BitmapImage unActiveBitmapImage = new BitmapImage(new Uri(DisabledSrc, UriKind.Relative));

        private BitmapImage favouriteBitmap = new BitmapImage(new Uri("/images/saved.png", UriKind.Relative));
        private BitmapImage unFavouriteBitmap = new BitmapImage(new Uri("/images/unsaved.png", UriKind.Relative));


        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo),
                typeof(IndicatorForMonitoring),
                new PropertyMetadata(null));

        public CommandInfo CommandInfo
        {
            get => (CommandInfo)GetValue(CommandInfoProperty);
            set
            {
                //MessageBox.ShowDialog($"CommandInfo: {CommandInfo}");
                SetValue(CommandInfoProperty, value);
            }
        }

        public static readonly DependencyProperty SavedImagePathProperty =
            DependencyProperty.Register(
                nameof(SavedImagePath),
                typeof(string),
                typeof(IndicatorForMonitoring),
                new PropertyMetadata(null));
        public string SavedImagePath
        {
            get => (string)GetValue(SavedImagePathProperty);
            set
            {
                SetValue(SavedImagePathProperty, value);
            }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(IndicatorForMonitoring),
                new PropertyMetadata("0"));
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, value);
            }
        }

        private readonly MonitoringManager _monitoringManager;
        private ILogicControllerObserver _logicControllerObserver;
        public ILogicControllerProvider _logicControllerProvider;
        private readonly IEventAggregator _eventAggregator;

        public IndicatorForMonitoring()
        {
            _monitoringManager = Bootstrapper.ContainerProvider.Resolve<MonitoringManager>();
            _eventAggregator = Bootstrapper.ContainerProvider.Resolve<IEventAggregator>();
            _logicControllerObserver = Bootstrapper.ContainerProvider.Resolve<ILogicControllerObserver>();
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ImageIndicator.Source = unActiveBitmapImage;

            if (_monitoringManager.IsCommandInFavourites(CommandInfo))
            {
                if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
                    BoolSavedIcon.Source = favouriteBitmap;
                else
                    DecimalSavedIcon.Source = favouriteBitmap;
            }
            else
            {
                if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
                    BoolSavedIcon.Source = unFavouriteBitmap;
                else
                    DecimalSavedIcon.Source = unFavouriteBitmap;
            }

            if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
            {
                BoolBlock.Visibility = Visibility.Visible;
                DecimalBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                BoolBlock.Visibility = Visibility.Collapsed;
                DecimalBlock.Visibility = Visibility.Visible;
            }

            _eventAggregator.GetEvent<OnCommandAddToFavouritesEvent>().Subscribe((command) =>
            {
                if (command != CommandInfo) return;
                if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
                    BoolSavedIcon.Source = favouriteBitmap;
                else
                    DecimalSavedIcon.Source = favouriteBitmap;
            });
            _eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Subscribe((command) =>
            {
                if (command != CommandInfo) return;
                if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
                    BoolSavedIcon.Source = unFavouriteBitmap;
                else
                    DecimalSavedIcon.Source = unFavouriteBitmap;
            });


            _logicControllerObserver.Subscribe(this, (data) =>
            {
                if (data.Value is bool)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if ((bool)data.Value)
                        {
                            ImageIndicator.Source = activeBitmapImage;
                        }
                        else
                        {
                            ImageIndicator.Source = unActiveBitmapImage;
                        }
                    });
                }
                else
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (data.Value != null && data.Value is IFormattable formattable)
                            Value = formattable.ToString("F2", CultureInfo.InvariantCulture);
                        else
                            Value = "N/A"; // или другое значение по умолчанию
                    });
                }
            }, CommandInfo);
        }

        private async void OnLikeIndicatorCallback(object sender, MouseButtonEventArgs e)
        {
            if (_monitoringManager.IsCommandInFavourites(CommandInfo))
            {
                // Спрашиваем подтверждение на удаление из избранного
                var result = await CustomMessageBox.ShowQuestionAsync(
                    "Удаление из избранного",
                    $"Вы действительно хотите удалить '{CommandInfo.Title}' из избранного?"
                );

                if (result == Models.MessageBoxResult.Yes)
                {
                    _monitoringManager.RemoveCommandFromFavourites(CommandInfo);
                }
            }
            else
            {
                // Спрашиваем подтверждение на добавление в избранное
                var result = await CustomMessageBox.ShowQuestionAsync(
                    "Добавление в избранное",
                    $"Вы действительно хотите добавить '{CommandInfo.Title}' в избранное?"
                );

                if (result == Models.MessageBoxResult.Yes)
                {
                    _monitoringManager.AddCommandToFavourites(CommandInfo);
                }
            }
        }
    }
}
