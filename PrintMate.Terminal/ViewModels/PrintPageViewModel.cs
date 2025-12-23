using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using HandyControl.Tools.Command;
using Microsoft.Win32;
using Opc2Lib;
using PrintMate.Terminal.Controls;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Hans.Events;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Models;
using DelegateCommand = Prism.Commands.DelegateCommand;
using Layer = Emgu.CV.Dnn.Layer;
using MessageBoxResult = PrintMate.Terminal.Models.MessageBoxResult;

namespace PrintMate.Terminal.ViewModels
{
    public class PrintPageViewModel : BindableBase
    {

        #region Приватные поля

        private readonly IEventAggregator _eventAggregator;
        private readonly CliProvider _cliProvider;

        private Project _currentProject;
        private int _currentLayer;
        private int _currentPrintingLayer;
        private int _maxLayers;
        private bool _isSimulating;
        private bool _isLoadingGeometry;
        private double _progressBarValue = 0;
        private double _printProgress = 0;
        private double _layerProgress = 0;
        private double _progressBarValueCurrentLayer = 0;
        private int _cachingProgress = 0;
        private double _cachingProgressPercent = 0;
        private Controls.DX11ViewportControl _viewport;
        private Controls.IsometricViewportControl _isometricViewport;
        private Controls.SkiaViewportControl _skiaViewport;
        private Controls.VeldridViewportControl _veldridViewport;
        private Views.LayerCanvas _layerCanvas;
        private bool _startPrintButtonIsEnabled = true;
        private bool _abortPrintButtonIsEnabled = true;
        private bool _startSingleLayerMarkButtonIsEnabled = true;
        private bool _startPowderApplyIsEnabled = true;
        private Visibility _startPrintVisibility = Visibility.Visible;
        private Visibility _continuePrintVisibility = Visibility.Collapsed;
        private Visibility _abortPrintVisibility = Visibility.Collapsed;
        private Visibility _pausePrintVisibility = Visibility.Collapsed;
        private Visibility _abortSingleLayerMarkVisibility = Visibility.Collapsed;
        private Visibility _startSingleLayerMarkVisibility = Visibility.Visible;
        private Visibility _shadowVisibility = Visibility.Visible;
        private string _startSingleLayerMarkButtonText;
        private string _recouterPositionString;
        private bool _isOnlineMode;
        private double _testScanner1Progress = 0;
        private double _testScanner2Progress = 0;
        #endregion

        #region Публичные свойства

        // ТЕСТОВЫЕ СВОЙСТВА ДЛЯ СЛАЙДЕРОВ
        public double TestScanner1Progress
        {
            get => _testScanner1Progress;
            set
            {
                if (SetProperty(ref _testScanner1Progress, value) && _isOnlineMode)
                {
                    _layerCanvas?.UpdateMarkingProgress(_testScanner1Progress, _testScanner2Progress);
                }
            }
        }

        public double TestScanner2Progress
        {
            get => _testScanner2Progress;
            set
            {
                if (SetProperty(ref _testScanner2Progress, value) && _isOnlineMode)
                {
                    _layerCanvas?.UpdateMarkingProgress(_testScanner1Progress, _testScanner2Progress);
                }
            }
        }

        public bool IsOnlineMode
        {
            get => _isOnlineMode;
            set
            {
                if (SetProperty(ref _isOnlineMode, value))
                {
                    OnOnlineModeChanged(value);
                }
            }
        }

        public bool IsSliderEnabled => !IsOnlineMode;


        public string RecouterPositionString
        {
            get => _recouterPositionString;
            set => SetProperty(ref _recouterPositionString, value);
        }
        public string StartSingleLayerMarkButtonText
        {
            get => _startSingleLayerMarkButtonText;
            set => SetProperty(ref _startSingleLayerMarkButtonText, value);
        }

        public Visibility ContinuePrintVisibility
        {
            get => _continuePrintVisibility;
            set => SetProperty(ref _continuePrintVisibility, value);
        }
        public Visibility ShadowVisibility
        {
            get => _shadowVisibility;
            set => SetProperty(ref _shadowVisibility, value);
        }

        public Visibility StartSingleLayerMarkVisibility
        {
            get => _startSingleLayerMarkVisibility;
            set => SetProperty(ref _startSingleLayerMarkVisibility, value);
        }

        public Visibility StartPrintVisibility
        {
            get => _startPrintVisibility;
            set => SetProperty(ref _startPrintVisibility, value);
        }

        public Visibility AbortPrintVisibility
        {
            get => _abortPrintVisibility;
            set => SetProperty(ref _abortPrintVisibility, value);
        }

        public Visibility PausePrintVisibility
        {
            get => _pausePrintVisibility;
            set => SetProperty(ref _pausePrintVisibility, value);
        }

        public Visibility AbortSingleLayerMarkVisibility
        {
            get => _abortSingleLayerMarkVisibility;
            set => SetProperty(ref _abortSingleLayerMarkVisibility, value);
        }

        public bool StartPrintButtonIsEnabled
        {
            get => _startPrintButtonIsEnabled;
            set => SetProperty(ref _startPrintButtonIsEnabled, value);
        }

        public bool AbortPrintButtonIsEnabled
        {
            get => _abortPrintButtonIsEnabled;
            set => SetProperty(ref _abortPrintButtonIsEnabled, value);
        }

        public bool StartSingleLayerMarkButtonIsEnabled
        {
            get => _startSingleLayerMarkButtonIsEnabled;
            set => SetProperty(ref _startSingleLayerMarkButtonIsEnabled, value);
        }

        public bool StartPowderApplyIsEnabled
        {
            get => _startPowderApplyIsEnabled;
            set => SetProperty(ref _startPowderApplyIsEnabled, value);
        }

        public Project Project
        {
            get => _currentProject;
            set => SetProperty(ref _currentProject, value);
        }

        private ObservableCollection<Part> _parts;
        public ObservableCollection<Part> Parts
        {
            get => _parts;
            set => SetProperty(ref _parts, value);
        }

        private Part _selectedPart;
        private int? _lastHighlightedPartId = null; // Кэш для избежания повторного выделения

        public Part SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (SetProperty(ref _selectedPart, value))
                {
                    OnPartSelected(value);
                }
            }
        }

        public bool IsLoadingGeometry
        {
            get => _isLoadingGeometry;
            set => SetProperty(ref _isLoadingGeometry, value);
        }

        public int CurrentLayer
        {
            get => _currentLayer;
            set
            {
                // PULL модель: просто обновляем значение, Viewport сам считает его в render loop
                SetProperty(ref _currentLayer, value);

                PrintProgress = (int)((value / (double)MaxLayers) * 100);

                // Обновляем только LayerCanvas (2D визуализация) - это легковесная операция
                if (_layerCanvas != null && _currentProject != null && value > 0 && value <= _currentProject.Layers.Count)
                {
                    _layerCanvas.LoadLayer(_currentProject.Layers[value - 1]);
                }
            }
        }
        public int CurrentPrintingLayer
        {
            get => _currentPrintingLayer;
            set => SetProperty(ref _currentPrintingLayer, value);
        }

        public int MaxLayers
        {
            get => _maxLayers;
            set => SetProperty(ref _maxLayers, value);
        }

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set => SetProperty(ref _progressBarValue, value);
        }

        public double ProgressBarValueCurrentLayer
        {
            get => _progressBarValueCurrentLayer;
            set => SetProperty(ref _progressBarValueCurrentLayer, value);
        }

        public double PrintProgress
        {
            get => _printProgress;
            set => SetProperty(ref _printProgress, value);
        }

        public double LayerProgress
        {
            get => _layerProgress;
            set => SetProperty(ref _layerProgress, value);
        }

        private int _singleLayerMarkProgress;
        public int SingleLayerMarkProgress
        {
            get => _singleLayerMarkProgress;
            set => SetProperty(ref _singleLayerMarkProgress, value);
        }

        public int CachingProgress
        {
            get => _cachingProgress;
            set => SetProperty(ref _cachingProgress, value);
        }

        public double CachingProgressPercent
        {
            get => _cachingProgressPercent;
            set => SetProperty(ref _cachingProgressPercent, value);
        }

        private string _projectName = "Проект не загружен";
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        // Общее время печати (разбито на часы, минуты, секунды для биндинга)
        private int _totalPrintHours;
        public int TotalPrintHours
        {
            get => _totalPrintHours;
            set => SetProperty(ref _totalPrintHours, value);
        }

        private int _totalPrintMinutes;
        public int TotalPrintMinutes
        {
            get => _totalPrintMinutes;
            set => SetProperty(ref _totalPrintMinutes, value);
        }

        private int _totalPrintSeconds;
        public int TotalPrintSeconds
        {
            get => _totalPrintSeconds;
            set => SetProperty(ref _totalPrintSeconds, value);
        }

        private int _selectLayerForSingleMark = 1;
        public int SelectLayerForSingleMark
        {
            get => _selectLayerForSingleMark;
            set
            {
                SetProperty(ref _selectLayerForSingleMark, value);
                StartSingleLayerMarkButtonText = $"Прожечь {value} слой";
            }
        }

        private int _doseCounts;

        public int DoseCounts
        {
            get => _doseCounts;
            set
            {
                SetProperty(ref _doseCounts, value, OnDoseCountsPropertyChanged);
            }
        }

        private async void OnDoseCountsPropertyChanged()
        {
            if (!_plcProvider.Connected)
            {
                return;
            }
            await _plcProvider.SetUInt32Async(OpcCommands.Set_Axes_DoserCounts, (uint)DoseCounts);
            _notificationService.Success("Успешно", $"Установленное количество доз - {DoseCounts}", 1);
        }

        #endregion

        #region Команды
        public RelayCommand StartPrintCommand { get; }
        public RelayCommand StopPrintCommand { get; }
        public RelayCommand PausePrintCommand { get; }
        public RelayCommand ContinuePrintCommand { get; }
        public RelayCommand SelectLayerForSingleMarkCommand { get; set; }
        public RelayCommand StartSingleLayerMarkCommand { get; set; }
        public RelayCommand StopSingleLayerMarkCommand { get; set; }
        public RelayCommand StartApplyPowderCommand { get; set; }
        public RelayCommand AddSingleLayerIndexCommand { get; set; }
        public RelayCommand SuddSingleLayerIndexCommand { get; set; }
        public RelayCommand SelectDoseCountsCommand { get; set; }
        public RelayCommand AddDoseCountsCommand { get; set; }
        public RelayCommand SuddDoseCountsCommand { get; set; }
        public RelayCommand<Part> SelectOrderCommand { get; set; }

        #endregion

        #region Конструктор

        private readonly PrintService _printService;
        private readonly ModalService _modalService;
        private readonly KeyboardService _keyboardService;
        private readonly NotificationService _notificationService;
        private readonly ILogicControllerObserver _plcObserver;
        private readonly ILogicControllerProvider _plcProvider;

        public PrintPageViewModel(
            IEventAggregator eventAggregator,
            PrintService printService,
            ModalService modalService,
            KeyboardService keyboardService,
            NotificationService notificationService,
            ILogicControllerObserver plcObserver,
            ILogicControllerProvider plcProvider
            )
        {
            _notificationService = notificationService;
            _keyboardService = keyboardService;
            _printService = printService;
            _modalService = modalService;
            _eventAggregator = eventAggregator;
            _cliProvider = new CliProvider();
            
            _plcObserver = plcObserver;
            _plcObserver.Subscribe(this, (responce) =>
            {
                if (responce.Value is int doseCount)
                {
                    DoseCounts = doseCount;
                }
            }, OpcCommands.Set_Axes_DoserCounts);
            _plcObserver.Subscribe(this, (responce) =>
            {
                if (responce.Value != null && responce.Value is bool isDoor && isDoor)
                {
                    RecouterPositionString = "У двери";
                } 
            }, OpcCommands.RecouterInDoorState);
            _plcObserver.Subscribe(this, (responce) =>
            {
                if (responce.Value != null && responce.Value is bool isHome && isHome)
                {
                    RecouterPositionString = "У дозатора";
                }
            }, OpcCommands.RecouterInHomeState);

            _plcProvider = plcProvider;

            // Инициализация коллекций
            Parts = new ObservableCollection<Part>();

            // Команды
            StartPrintCommand = new RelayCommand(StartPrintCommandCallback);
            StopPrintCommand = new RelayCommand(StopPrintCommandDelegate);
            PausePrintCommand = new RelayCommand(PausePrintCommandCallback);
            ContinuePrintCommand = new RelayCommand(ContinuePrintCommandCallback);
            SelectLayerForSingleMarkCommand = new RelayCommand(SelectLayerForSingleMarkCommandCallback);
            StartSingleLayerMarkCommand = new RelayCommand(StartSingleLayerMarkCommandCallback);
            StopSingleLayerMarkCommand = new RelayCommand(StopSingleMarkingLayerCallback);
            StartApplyPowderCommand = new RelayCommand(ApplyLayerPowder);
            AddSingleLayerIndexCommand = new RelayCommand(OnAddSingleLayerIndexButtonCallback);
            SuddSingleLayerIndexCommand = new RelayCommand(OnSuddSingleLayerIndexButtonCallback);
            SelectDoseCountsCommand = new RelayCommand(SelectDoseCountsButtonCallback);
            AddDoseCountsCommand = new RelayCommand(AddDoseCountsButtonCallback);
            SuddDoseCountsCommand = new RelayCommand(SuddDoseCountsButtonCallback);
            SelectOrderCommand = new RelayCommand<Part>(OnPartSelected);

            StartSingleLayerMarkButtonText = $"Прожечь {SelectLayerForSingleMark} слой";

            // На случай, если не модель не успела инициализироваться, а  проект уже загружен
            LoadProject();
            // Ловим событие выбора проекта
            _eventAggregator.GetEvent<OnActiveProjectSelected>().Subscribe((p) =>
            {
                LoadProject();
            });

            // Обрабатываем завершение маркировки слоя
            _eventAggregator.GetEvent<OnLayerMarkFinish>().Subscribe((layer) =>
            {
                var currentLayer = layer.Id;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ProgressBarValue = ((double)currentLayer / (double)MaxLayers) * 100d;
                    PrintProgress = (int)ProgressBarValue;
                    return CurrentLayer = currentLayer;
                });
            });

            // Обработка прогресса маркировки
            _eventAggregator.GetEvent<OnMarkingProgressEvent>().Subscribe((e) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    //Console.WriteLine("OnMarkingProgressEvent");
                    ProgressBarValueCurrentLayer = _printService.GetLayerProgress();
                    LayerProgress = (int)_printService.GetLayerProgress();

                    // Обновляем визуализацию онлайн-режима
                    if (_isOnlineMode)
                    {
                        UpdateOnlineVisualization();
                    }
                });
            });

            // Обработка прогресса маркировки при прожиге конкретного слоя
            _eventAggregator.GetEvent<OnSingleModeMarkingProgressEvent>().Subscribe((e) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    //Console.WriteLine("OnSingleModeMarkingProgressEvent");
                    SingleLayerMarkProgress = (int)_printService.GetLayerProgress();
                    //LayerProgress = (int)_printService.GetLayerProgress();
                });
            });

            // Завершение маркировки при прожиге конкретного слоя
            _eventAggregator.GetEvent<OnSingleLayerPrintFinishedEvent>().Subscribe(async (layerId) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SingleLayerMarkProgress = 100;
                    CustomMessageBox.ShowSuccessAsync("Успешно", $"Прожиг {SelectLayerForSingleMark} слоя завершен");

                    LayerProgress = 0;
                    IsOnlineMode = false; // Сбрасываем онлайн-режим, чтобы модель снова отображалась
                    StartSingleLayerMarkButtonIsEnabled = true;
                    SingleLayerMarkProgress = 0;
                    StartPrintButtonIsEnabled = true;
                    AbortSingleLayerMarkVisibility = Visibility.Collapsed;
                    StartPowderApplyIsEnabled = true;
                });
            });

            _eventAggregator.GetEvent<OnLayerPrintStart>().Subscribe((layer =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPrintingLayer = layer.Id;
                    CurrentLayer = layer.Id;
                });
            }));
            _eventAggregator.GetEvent<OnLayerPrintFinish>().Subscribe(async (layer) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPrintingLayer = layer.Id;
                    CurrentLayer = layer.Id;
                });
            });

            // Обработка изменения состояния сервиса печати
            HandlePrintServiceStateChanged(_printService.State);
            // ловим событие ёизменения состояния сервиса печати
            _eventAggregator.GetEvent<OnPrintServiceStateChangedEvent>().Subscribe(HandlePrintServiceStateChanged);

            // Подписываемся на событие изменения проекта (удаление/изменение деталей)
            _eventAggregator.GetEvent<OnProjectModifiedEvent>().Subscribe(OnProjectModified);

            if (_printService.ActiveProject != null)
            {
                CurrentLayer = _printService.ActiveProject.CurrentLayer.Id;
            }

            Task.Factory.StartNew(async () =>
            {
                if (_printService.ActiveProject != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CurrentLayer = _printService.ActiveProject.CurrentLayer.Id;
                    });
                }
                await Task.Delay(5000);
            });
            _eventAggregator.GetEvent<OnProjectFinishedEvent>().Subscribe(() =>
            {
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    CurrentLayer = 0;
                    LayerProgress = 0;
                    PrintProgress = 0;
                    CurrentPrintingLayer = 0;

                    AbortPrintVisibility = Visibility.Collapsed;
                    PausePrintVisibility = Visibility.Collapsed;
                    StartSingleLayerMarkButtonIsEnabled = true;
                    StartPowderApplyIsEnabled = true;
                    StartPrintButtonIsEnabled = true;
                    StartPrintVisibility = Visibility.Visible;
                    await CustomMessageBox.ShowSuccessAsync("Печать завершена", "");
                });
            });
        }


        private void AddDoseCountsButtonCallback(object obj)
        {
            if (!_plcProvider.Connected)
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }
            DoseCounts++;
        }

        private void SuddDoseCountsButtonCallback(object obj)
        {
            if (!_plcProvider.Connected)
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }
            if ((DoseCounts - 1) >= 0)
            {
                DoseCounts--;
            } 
        }

        private void SelectDoseCountsButtonCallback(object obj)
        {
            if (!_plcProvider.Connected)
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }
            var result = _keyboardService.Show(KeyboardType.Numpad, "Введите количество доз", DoseCounts.ToString());
            if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int intResult))
            {
                DoseCounts = intResult;
            }
        }

        private void OnAddSingleLayerIndexButtonCallback(object obj)
        {
            if (!_printService.IsScanBoardsReady())
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к маркировочной системе");
                return;
            }
            if ((SelectLayerForSingleMark + 1) <= MaxLayers)
            {
                SelectLayerForSingleMark++;
            }
        }

        private void OnSuddSingleLayerIndexButtonCallback(object obj)
        {
            if (!_printService.IsScanBoardsReady())
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к маркировочной системе");
                return;
            }
            if ((SelectLayerForSingleMark - 1) >= 1)
            {
                SelectLayerForSingleMark--;
            }
        }

        private void ApplyLayerPowder(object obj)
        {
            if (!_plcProvider.Connected)
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }
            StartPowderApplyIsEnabled = false;
            _ = Task.Run(async () =>
            {
                await _printService.ApplyLayerPowder();
                await Application.Current.Dispatcher.InvokeAsync(() => StartPowderApplyIsEnabled = true);
            }, new CancellationTokenSource(60000).Token);

        }

        private void LoadProject()
        {
            try
            {
                if (_printService.ActiveProject != null &&
                    (_currentProject == null ||
                     _currentProject.ProjectInfo?.Name != _printService.ActiveProject.ProjectInfo?.Name))
                {
                    var partsList = _printService.ActiveProject.HeaderInfo?.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
                    if (partsList != null && partsList.Count > 0)
                    {
                        Parts = new ObservableCollection<Part>(partsList);
                    }
                    else
                    {
                        Parts = new ObservableCollection<Part>();
                    }
                    LoadProjectInternal(_printService.ActiveProject);
                    CurrentLayer = 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        private async void StopSingleMarkingLayerCallback(object e)
        {
            var result = await CustomMessageBox.ShowQuestionAsync("Остановка печати", "Вы действительно хотите прервать печать ?");
            if (result == MessageBoxResult.Yes)
            {
                _ = Task.Run(_printService.StopSingleLayer);

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LayerProgress = 0;
                    IsOnlineMode = false; // Сбрасываем онлайн-режим, чтобы модель снова отображалась
                    CurrentPrintingLayer = 0;
                    AbortSingleLayerMarkVisibility = Visibility.Collapsed;
                    StartPrintButtonIsEnabled = true;
                });
            }
        }

        private async void ContinuePrintCommandCallback(object e)
        {
            await _printService.Continue();
        }

        private async void PausePrintCommandCallback(object e)
        {
            await _printService.Pause();
        }

        private void HandlePrintServiceStateChanged(PrintServiceState state)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                switch (state)
                {
                    case PrintServiceState.Started:
                        StartPrintVisibility = Visibility.Collapsed;
                        AbortPrintVisibility = Visibility.Visible;
                        PausePrintVisibility = Visibility.Visible;
                        StartSingleLayerMarkButtonIsEnabled = false;
                        StartPowderApplyIsEnabled = false;
                        break;
                    case PrintServiceState.Pause:
                        ContinuePrintVisibility = Visibility.Visible;
                        AbortPrintVisibility = Visibility.Visible;
                        PausePrintVisibility = Visibility.Collapsed;
                        break;
                    case PrintServiceState.Stop:
                        StartPrintVisibility = Visibility.Visible;
                        ContinuePrintVisibility = Visibility.Collapsed;
                        AbortPrintVisibility = Visibility.Collapsed;
                        PausePrintVisibility = Visibility.Collapsed;
                        StartSingleLayerMarkButtonIsEnabled = true;
                        StartPowderApplyIsEnabled = true;
                        break;
                }
            });
        }

        private async void StartSingleLayerMarkCommandCallback(object e)
        {
            try
            {
                if (SelectLayerForSingleMark <= 0)
                {
                    _notificationService.Error("Ошибка", "Номер слоя для прожига должен быть больше нуля.");
                    return;
                }
                if (SelectLayerForSingleMark > MaxLayers)
                {
                    _notificationService.Error("Ошибка", "Номер слоя для прожига не может привышать количество слоев в проекте.");
                    return;
                }
                if (!_printService.IsScanBoardsReady())
                {
                    _notificationService.Error("Ошибка", "Ошибка подключения к сканаторным системам");
                    return;
                }
                if (!await _printService.IsReadyToPrintLayer())
                {
                    await _printService.ShowErrorReasonForUser();
                    return;
                }

                var result = await CustomMessageBox.ShowQuestionAsync("Запуск прожига", $"Вы действительно хотите запустить прожиг {SelectLayerForSingleMark} слоя ?");
                if (result == MessageBoxResult.Yes)
                {
                    StartPrintButtonIsEnabled = false;
                    StartSingleLayerMarkButtonIsEnabled = true;
                    AbortSingleLayerMarkVisibility = Visibility.Visible;
                    AbortPrintVisibility = Visibility.Collapsed;
                    PausePrintVisibility = Visibility.Collapsed;
                    StartPowderApplyIsEnabled = false;

                    await _printService.StartMarkSingleLayer(SelectLayerForSingleMark);
                    _notificationService.Success("Успешно", $"Прожиг {SelectLayerForSingleMark} начался.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.Error("Ошибка", $"Неизвестная ошибка при запуске");
                Console.WriteLine(ex);
                throw;
            }

        }

        private async void SelectLayerForSingleMarkCommandCallback(object e)
        {
            if (!_printService.IsScanBoardsReady())
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к маркировочной системе");
                return;
            }
            var result = _keyboardService.Show(KeyboardType.Numpad, "Введите номер слоя", SelectLayerForSingleMark.ToString());
            if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int intResult))
            {
                SelectLayerForSingleMark = intResult;
            }
        }

        private async void StopPrintCommandDelegate(object e)
        {
            var currentPrintServiceState = await _printService.GetState();
            if (currentPrintServiceState == PrintServiceState.Started ||
                currentPrintServiceState == PrintServiceState.Pause)
            {
                var result = await CustomMessageBox.ShowQuestionAsync("Остановка печати", "Вы действительно хотите прервать печать ?");
                if (result == MessageBoxResult.Yes)
                {
                    _ = Task.Run(_printService.Stop);

                    CurrentPrintingLayer = 0;
                    PrintProgress = 0;
                    ProgressBarValue = 0;
                    CurrentLayer = 0;
                    LayerProgress = 0;
                    IsOnlineMode = false; // Сбрасываем онлайн-режим, чтобы модель снова отображалась
                    ProgressBarValueCurrentLayer = 0;
                    StartPrintVisibility = Visibility.Visible;
                    AbortPrintVisibility = Visibility.Collapsed;
                    PausePrintVisibility = Visibility.Collapsed;
                    StartSingleLayerMarkButtonIsEnabled = true;
                    StartPowderApplyIsEnabled = true;
                }
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Печать проекта не запущена");
            }
        }

        private async void StartPrintCommandCallback(object e)
        {
            if (!_plcProvider.Connected)
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }
            if (!_printService.IsScanBoardsReady())
            {
                _notificationService.Error("Ошибка", "Отсутствует подключение к маркировочной системе");
                return;
            }
            if (_printService.ActiveProject == null)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Проект не выбран");
                return;
            }
            if (!await _printService.IsReadyToPrintLayer())
            {
                await _printService.ShowErrorReasonForUser();
                return;
            }

            var currentPrintServiceState = await _printService.GetState();

            if (currentPrintServiceState == PrintServiceState.Stop)
            {
                var result = await CustomMessageBox.ShowQuestionAsync("Запуск печати", "Вы действительно хотите запустить печать проекта ?");
                if (result == MessageBoxResult.Yes)
                {
                    await _printService.StartPrint();

                    StartPrintButtonIsEnabled = false;
                    AbortPrintVisibility = Visibility.Visible;
                    PausePrintVisibility = Visibility.Visible;
                    StartSingleLayerMarkButtonIsEnabled = false;
                    StartPowderApplyIsEnabled = false;
                }
            }
            else if (currentPrintServiceState == PrintServiceState.Started)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Печать уже началась");
            }
            else if (currentPrintServiceState == PrintServiceState.Pause)
            {
                var result = await CustomMessageBox.ShowQuestionAsync("Возобновить печать", "Вы действительно хотите возобновить печать проекта ?");
                if (result == MessageBoxResult.Yes)
                {
                    await _printService.Continue();
                }
            }
        }

        #endregion

        #region Методы

        private void LoadProjectInternal(Project project)
        {
            // Защита от повторной загрузки того же проекта
            // Сравниваем по имени проекта, т.к. ID не уникален
            if (_currentProject != null && project != null &&
                _currentProject.ProjectInfo?.Name == project.ProjectInfo?.Name)
            {
                //Console.WriteLine($"[ProjectViewer3D] Project already loaded (Name: {project.ProjectInfo?.Name}), skipping");
                return;
            }

            _currentProject = project;

            // Устанавливаем имя проекта
            ProjectName = project?.ProjectInfo?.Name ?? "Без имени";

            // Вычисляем время печати из проекта
            CalculatePrintTime(project);

            // Загружаем Parts из заголовка проекта
            var partsList = project?.HeaderInfo?.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
            //Console.WriteLine($"[ProjectViewer3D] Parts from header in LoadProjectInternal: {partsList?.Count ?? 0}");

            if (partsList != null && partsList.Count > 0)
            {
                Parts = new ObservableCollection<Part>(partsList);
                //Console.WriteLine($"[ProjectViewer3D] Parts loaded in LoadProjectInternal: {Parts.Count}");
            }
            else
            {
                Parts = new ObservableCollection<Part>();
                //Console.WriteLine($"[ProjectViewer3D] No parts found in header in LoadProjectInternal");
            }

            if (project?.Layers != null && project.Layers.Count > 0)
            {
                MaxLayers = project.Layers.Count;

                // Передаём проект в viewport (DX11 - основной)
                _viewport?.LoadProject(project);

                // Устанавливаем первый слой (это вызовет UpdateVisualization через setter)
                CurrentLayer = 1;

                Console.WriteLine($"[ProjectViewer3D] Project loaded: {project.ProjectInfo?.Name}, {project.Layers.Count} layers");
            }
        }

        /// <summary>
        /// Вычисляет время печати из проекта и разбивает на часы, минуты и секунды
        /// </summary>
        private void CalculatePrintTime(Project project)
        {
            TotalPrintHours = 0;
            TotalPrintMinutes = 0;
            TotalPrintSeconds = 0;

            if (project == null)
                return;

            try
            {
                double totalSeconds = project.GetPrintTimeInSeconds();
                var timeSpan = TimeSpan.FromSeconds(totalSeconds);

                TotalPrintHours = (int)timeSpan.TotalHours;
                TotalPrintMinutes = timeSpan.Minutes;
                TotalPrintSeconds = timeSpan.Seconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintPageViewModel] Error calculating print time: {ex.Message}");
            }
        }

        /// <summary>
        /// Устанавливает VeldridViewport (GPU рендерер через OpenGL)
        /// </summary>
        public void SetVeldridViewport(Controls.VeldridViewportControl viewport)
        {
            _veldridViewport = viewport;
            Console.WriteLine("[ProjectViewer3D] VeldridViewport set");

            // PULL модель: передаём функцию для получения текущего слоя
            _veldridViewport.SetCurrentLayerGetter(() => CurrentLayer);

            // Подключаем callback для отслеживания состояния загрузки
            _veldridViewport.OnLoadingStateChanged = (isLoading) =>
            {
                IsLoadingGeometry = isLoading;
            };

            // Подключаем callback для обработки кликов по деталям
            _veldridViewport.OnPartClicked = (partId) =>
            {
                OnPartClickedFromViewport(partId);
            };

            // Подключаем callback для отслеживания прогресса кеширования
            _veldridViewport.OnCachingProgress = (maxCachedLayer) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CachingProgress = maxCachedLayer;
                    if (MaxLayers > 0)
                    {
                        CachingProgressPercent = (double)maxCachedLayer / MaxLayers * 100.0;
                    }
                });
            };

            // Если проект уже загружен, передаём его в viewport
            if (_currentProject != null)
            {
                _veldridViewport.LoadProject(_currentProject);
            }
            // Если проект ещё не загружен, но есть активный проект в сервисе - загружаем его
            else if (_printService.ActiveProject != null)
            {
                Console.WriteLine("[ProjectViewer3D] Loading active project on veldrid viewport set");
                LoadProjectInternal(_printService.ActiveProject);
            }
        }

        /// <summary>
        /// Устанавливает SkiaViewport (GPU рендерер через SkiaSharp/OpenGL)
        /// </summary>
        public void SetSkiaViewport(Controls.SkiaViewportControl viewport)
        {
            _skiaViewport = viewport;
            Console.WriteLine("[ProjectViewer3D] SkiaViewport set");

            // PULL модель: передаём функцию для получения текущего слоя
            _skiaViewport.SetCurrentLayerGetter(() => CurrentLayer);

            // Подключаем callback для отслеживания состояния загрузки
            _skiaViewport.OnLoadingStateChanged = (isLoading) =>
            {
                IsLoadingGeometry = isLoading;
            };

            // Подключаем callback для обработки кликов по деталям
            _skiaViewport.OnPartClicked = (partId) =>
            {
                OnPartClickedFromViewport(partId);
            };

            // Подключаем callback для отслеживания прогресса кеширования
            _skiaViewport.OnCachingProgress = (maxCachedLayer) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CachingProgress = maxCachedLayer;
                    if (MaxLayers > 0)
                    {
                        CachingProgressPercent = (double)maxCachedLayer / MaxLayers * 100.0;
                    }
                });
            };

            // Если проект уже загружен, передаём его в viewport
            if (_currentProject != null)
            {
                _skiaViewport.LoadProject(_currentProject);
            }
            // Если проект ещё не загружен, но есть активный проект в сервисе - загружаем его
            else if (_printService.ActiveProject != null)
            {
                Console.WriteLine("[ProjectViewer3D] Loading active project on skia viewport set");
                LoadProjectInternal(_printService.ActiveProject);
            }
        }

        /// <summary>
        /// Устанавливает IsometricViewport (CPU рендерер для слабых ПК)
        /// </summary>
        public void SetIsometricViewport(Controls.IsometricViewportControl viewport)
        {
            _isometricViewport = viewport;
            Console.WriteLine("[ProjectViewer3D] IsometricViewport set");

            // PULL модель: передаём функцию для получения текущего слоя
            _isometricViewport.SetCurrentLayerGetter(() => CurrentLayer);

            // Подключаем callback для отслеживания состояния загрузки
            _isometricViewport.OnLoadingStateChanged = (isLoading) =>
            {
                IsLoadingGeometry = isLoading;
            };

            // Подключаем callback для обработки кликов по деталям
            _isometricViewport.OnPartClicked = (partId) =>
            {
                OnPartClickedFromViewport(partId);
            };

            // Подключаем callback для отслеживания прогресса кеширования
            _isometricViewport.OnCachingProgress = (maxCachedLayer) =>
            {
                // Обновляем максимальный доступный слой
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CachingProgress = maxCachedLayer;
                    if (MaxLayers > 0)
                    {
                        CachingProgressPercent = (double)maxCachedLayer / MaxLayers * 100.0;
                    }
                });
            };

            // Если проект уже загружен, передаём его в viewport
            if (_currentProject != null)
            {
                _isometricViewport.LoadProject(_currentProject);
            }
            // Если проект ещё не загружен, но есть активный проект в сервисе - загружаем его
            else if (_printService.ActiveProject != null)
            {
                Console.WriteLine("[ProjectViewer3D] Loading active project on isometric viewport set");
                LoadProjectInternal(_printService.ActiveProject);
            }
        }

        public void SetViewport(Controls.DX11ViewportControl viewport)
        {
            _viewport = viewport;

            // PULL модель: передаём функцию для получения текущего слоя
            _viewport.SetCurrentLayerGetter(() => CurrentLayer);

            // Подключаем callback для отслеживания состояния загрузки
            _viewport.OnLoadingStateChanged = (isLoading) =>
            {
                IsLoadingGeometry = isLoading;
            };

            // Подключаем callback для обработки кликов по деталям
            _viewport.OnPartClicked = (partId) =>
            {
                OnPartClickedFromViewport(partId);
            };

            // Подключаем callback для отслеживания прогресса кеширования
            _viewport.OnCachingProgress = (maxCachedLayer) =>
            {
                // Обновляем максимальный доступный слой
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CachingProgress = maxCachedLayer;
                    if (MaxLayers > 0)
                    {
                        CachingProgressPercent = (double)maxCachedLayer / MaxLayers * 100.0;
                    }
                });
            };

            // Если проект уже загружен, передаём его в viewport
            if (_currentProject != null)
            {
                _viewport.LoadProject(_currentProject);
            }
            // Если проект ещё не загружен, но есть активный проект в сервисе - загружаем его
            else if (_printService.ActiveProject != null)
            {
                LoadProjectInternal(_printService.ActiveProject);
            }
        }

        public void SetLayerCanvas(Views.LayerCanvas layerCanvas)
        {
            _layerCanvas = layerCanvas;
            //Console.WriteLine("[ProjectViewer3D] LayerCanvas set");

            // Если проект уже загружен, передаём текущий слой в LayerCanvas
            if (_currentProject != null && _currentLayer > 0 && _currentLayer <= _currentProject.Layers.Count)
            {
                _layerCanvas.LoadLayer(_currentProject.Layers[_currentLayer - 1]);
            }
            // Если проект ещё не загружен, но есть активный проект в сервисе - он уже будет загружен через SetViewport
        }


        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Проверяем, был ли передан проект через навигационные параметры
            if (navigationContext.Parameters.ContainsKey("project"))
            {
                var project = navigationContext.Parameters["project"] as Project;
                if (project != null)
                {
                    //Console.WriteLine("[ProjectViewer3D] Project received via navigation parameters");
                    LoadProjectInternal(project);
                }
            }

            // Получаем viewport из view
            if (navigationContext.NavigationService.Region.ActiveViews != null)
            {
                foreach (var view in navigationContext.NavigationService.Region.ActiveViews)
                {
                    if (view is Views.Pages.ProjectViewer3D viewer3D)
                    {
                        //_viewport = viewer3D.GetViewport();
                        //Console.WriteLine("[ProjectViewer3D] Viewport connected");

                        // Если проект уже загружен, передаём его в viewport
                        if (_currentProject != null)
                        {
                            //_viewport.LoadProject(_currentProject);
                        }
                        break;
                    }
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        #endregion

        #region Обработка выбора детали

        private void OnPartSelected(Part part)
        {
            if (part == null)
            {
                // Сбрасываем выделение - показываем все детали обычным цветом
                _layerCanvas?.HighlightPart(null);
                return;
            }

            // Открываем модальное окно для управления деталью
            OpenPartManagementModal(part);
        }

        /// <summary>
        /// Открывает модальное окно для управления деталью
        /// </summary>
        private void OpenPartManagementModal(Part part)
        {
            try
            {
                var options = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "Part", part }
                };

                _modalService.Show<Views.Modals.PartManagementModal, PartManagementModalViewModel>(
                    modalId: $"PartManagement_{part.Id}",
                    options: options,
                    showOverlay: true,
                    closeOnBackgroundClick: true);
            }
            catch (Exception ex)
            {
                _notificationService.Error("Ошибка", $"Не удалось открыть окно управления деталью: {ex.Message}");
                Console.WriteLine($"[PrintPageView] Error opening part management modal: {ex}");
            }
        }

        /// <summary>
        /// Обработчик клика по детали в 3D viewport
        /// </summary>
        private void OnPartClickedFromViewport(int? partId)
        {
            //if (!partId.HasValue)
            //{
            //    // Клик по фону - сбрасываем выделение
            //    SelectedPart = null;
            //    return;
            //}

            //// Отладочный вывод состояния коллекции Parts
            ////Console.WriteLine($"[ProjectViewer3D] OnPartClickedFromViewport called with partId={partId.Value}");
            ////Console.WriteLine($"[ProjectViewer3D] Parts collection count: {Parts?.Count ?? 0}");
            //if (Parts != null && Parts.Count > 0)
            //{
            //    //Console.WriteLine($"[ProjectViewer3D] Parts IDs: {string.Join(", ", Parts.Select(p => $"{p.Id}:{p.Name}"))}");
            //}

            //// Ищем деталь по ID в коллекции Parts
            //var part = Parts?.FirstOrDefault(p => p.Id == partId.Value);

            //if (part != null)
            //{
            //    //Console.WriteLine($"[ProjectViewer3D] Part clicked in viewport: {part.Name} (ID: {part.Id})");
            //    // Устанавливаем SelectedPart - это автоматически вызовет OnPartSelected через setter
            //    SelectedPart = part;
            //}
            //else
            //{
            //    //Console.WriteLine($"[ProjectViewer3D] Part with ID {partId.Value} not found in Parts collection");

            //    // Пытаемся загрузить Parts из проекта, если они не загружены
            //    if (_currentProject?.HeaderInfo != null)
            //    {
            //        var partsList = _currentProject.HeaderInfo.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
            //        if (partsList != null && partsList.Count > 0)
            //        {
            //            //Console.WriteLine($"[ProjectViewer3D] Reloading Parts from project header: {partsList.Count} parts");
            //            Parts = new ObservableCollection<Part>(partsList);

            //            // Повторная попытка найти деталь
            //            part = Parts.FirstOrDefault(p => p.Id == partId.Value);
            //            if (part != null)
            //            {
            //                //Console.WriteLine($"[ProjectViewer3D] Part found after reload: {part.Name} (ID: {part.Id})");
            //                SelectedPart = part;
            //            }
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Обработчик события изменения проекта (удаление/изменение деталей)
        /// </summary>
        private void OnProjectModified(string modificationType)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Обновляем список деталей из HeaderInfo
                    if (_printService.ActiveProject?.HeaderInfo != null)
                    {
                        var partsList = _printService.ActiveProject.HeaderInfo.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
                        if (partsList != null)
                        {
                            Parts = new ObservableCollection<Part>(partsList);
                        }
                        else
                        {
                            Parts = new ObservableCollection<Part>();
                        }
                    }

                    // Обновляем визуализацию LayerCanvas
                    if (_layerCanvas != null && _currentProject != null && _currentLayer > 0 && _currentLayer <= _currentProject.Layers.Count)
                    {
                        _layerCanvas.LoadLayer(_currentProject.Layers[_currentLayer - 1]);
                    }

                    Console.WriteLine($"[PrintPageView] Project modified: {modificationType}, Parts count: {Parts?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PrintPageView] Error updating after project modification: {ex}");
                }
            });
        }

        #endregion

        #region Онлайн-режим прожига

        /// <summary>
        /// Обработчик изменения режима онлайн-отображения
        /// </summary>
        private void OnOnlineModeChanged(bool isOnline)
        {
            if (_layerCanvas == null)
                return;

            if (isOnline)
            {
                // Включаем онлайн-режим - скрываем основной слой и показываем прогресс
                if (LayerProgress > 0)
                {
                    // Если есть прогресс, обновляем визуализацию
                    UpdateOnlineVisualization();
                }
                else
                {
                    // Если прогресса нет, просто включаем режим (скрываем фигуру)
                    _layerCanvas.UpdateMarkingProgress(0.0);
                }
            }
            else
            {
                // Выключаем онлайн-режим - сбрасываем подсветку и показываем основной слой
                _layerCanvas.ClearMarkingProgress();
            }
        }

        /// <summary>
        /// Обновление визуализации в онлайн-режиме
        /// </summary>
        private void UpdateOnlineVisualization()
        {
            if (_layerCanvas == null || _currentProject == null)
                return;

            // Получаем раздельные прогрессы сканаторов
            double scanner1Progress = _printService.GetScanner1Progress(); // Сканатор 1 (227, LaserNum=1)
            double scanner2Progress = _printService.GetScanner2Progress(); // Сканатор 2 (228, LaserNum=0)

            // Передаём раздельные прогрессы в LayerCanvas
            _layerCanvas.UpdateMarkingProgress(scanner1Progress, scanner2Progress);
        }

        #endregion
    }
}
