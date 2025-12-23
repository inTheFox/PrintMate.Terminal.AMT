using Emgu.CV.Dnn;
using HansScannerHost.Models;
using MaterialDesignThemes.Wpf.Converters;
using Microsoft.AspNetCore.Authorization;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Opc;
using PrintSpectator.Shared.Models;
using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Serialization;
using Layer = ProjectParserTest.Parsers.Shared.Models.Layer;

namespace PrintMate.Terminal.Services
{
    public enum PrintServiceMode
    {
        Automatic,
        Manual,
    }
    public enum PrintServiceState
    {
        Stop,
        Started,
        Pause
    }
    public class PrintService
    {
        public static PrintService Instance = null;
        public Project ActiveProject = null;
        public Layer? CurrentLayer = null;
        public bool IsFirstLayerPrintOnly = false;
        public PrintServiceState State { get; private set; } = PrintServiceState.Stop;
        public PrintServiceMode Mode { get; private set; }

        private readonly IEventAggregator _eventAggregator;
        private readonly MultiScanatorSystemProxy _multiScanatorSystemProxy;
        private readonly ILogicControllerObserver _logicControllerObserver;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly PrintSessionService _printSessionService;
        private readonly LoggerService _loggerService;
        private readonly AuthorizationService _authorizationService;
        private readonly NotificationService _notificationService;

        private CancellationTokenSource _abortPrintCancellationTokenSource = null;
        private Task _startMarkTask = null;

        #region Условные переменные для начала печати
        // Готовность газовой системы
        private bool _readyGasSystem;

        // Готовность лазерной системы
        private bool _readyLaserSystem;

        // Готовность нагревателя
        private bool _readyHeatingTable;

        // Отсутствие ошибок блокировки
        private bool _notErrorsBlockingPrinting;

        // Ошибка реферирования рекоутера
        private bool _recoaterRefError;

        // Ошибка привода рекоутера
        private bool _recoaterEngineError;

        // Ошибка привода дозатора
        private bool _doserAxesError;

        // Ошибка привода платформы
        private bool _platformAxesError;
        #endregion

        private bool _isDosingStarted = true;
        private bool _printLayerStart = false;
        private AutomaticProcessSettings _automaticProcessSettings;

        public PrintService(
            IEventAggregator eventAggregator,
            MultiScanatorSystemProxy multiScanatorSystemProxy,
            ILogicControllerObserver logicControllerObserver,
            ILogicControllerProvider logicControllerProvider,
            PrintSessionService printSessionService,
            LoggerService loggerService,
            AuthorizationService authorizationService,
            NotificationService notificationService
            )
        {
            Instance = this;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
            _loggerService = loggerService;
            _logicControllerObserver = logicControllerObserver;
            _logicControllerProvider = logicControllerProvider;
            _multiScanatorSystemProxy = multiScanatorSystemProxy;
            _eventAggregator = eventAggregator;
            _printSessionService = printSessionService;
            _eventAggregator.GetEvent<OnLayerMarkFinish>().Subscribe((layer)=> Task.Run(async ()=> await OnLayerScanFinish(layer)));
            _automaticProcessSettings = Bootstrapper.Configuration.Get<AutomaticProcessSettings>();
        }

        /// <summary>
        /// Колбэк на завершение сканирования
        /// </summary>
        /// <returns></returns>
        private async Task OnLayerScanFinish(Layer layer)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("hh:mm:ss")}] OnLayerScanFinish");
            try
            {
                if (Mode == PrintServiceMode.Manual)
                {
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        _eventAggregator.GetEvent<OnSingleLayerPrintFinishedEvent>().Publish(0);
                        await _loggerService.InformationAsync(this, $"Прожиг #{layer.Id} слоя завершен");
                        _eventAggregator.GetEvent<OnLayerPrintFinish>().Publish(layer);
                    });
                    Mode = PrintServiceMode.Automatic;
                    return;
                }
                await ComSetIsMarking(false);


                Console.WriteLine($"Маркировка #{layer.Id} слоя завершена");
                await _loggerService.InformationAsync(this, $"Маркировка #{layer.Id} слоя завершена");
                _eventAggregator.GetEvent<OnLayerPrintFinish>().Publish(layer);


                // Фиксируем завершение слоя в сессии
                await _printSessionService.FinishLayerAsync();

                if (ActiveProject.IsLastLayer())
                {
                    Console.WriteLine("Печать проекта завершена");
                    await _loggerService.InformationAsync(this, $"Печать проекта успешно завершена");
                    // Завершаем сессию печати успешно
                    await _printSessionService.FinishSessionAsync();
                    _eventAggregator.GetEvent<OnProjectFinishedEvent>().Publish();
                    return;
                }

                //bool printLayerStart = await Application.Current.Dispatcher.InvokeAsync(() => _printLayerStart);
                if (_startMarkTask != null && !_startMarkTask.IsCompleted)
                {
                    while (true)
                    {
                        //printLayerStart = await Application.Current.Dispatcher.InvokeAsync(() => _printLayerStart);
                        if (_abortPrintCancellationTokenSource != null &&
                            _abortPrintCancellationTokenSource.Token.IsCancellationRequested) return;
                        
                        if (_startMarkTask.IsCompleted == true) break;
                        Console.WriteLine("Ожидаем конца предыдущего слоя");
                        await Task.Delay(500);
                    }
                }

                if (State == PrintServiceState.Started)
                {
                    ActiveProject.NextLayer();
                    await PrintCurrentLayer();
                }
                else if (State == PrintServiceState.Pause)
                {
                    await CustomMessageBox.ShowSuccessAsync("Успешно", "Печать проекта поставлена на паузу");
                    await _loggerService.InformationAsync(this, $"Печать проекта успешно поставлена на паузу");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Запуск маркировки без циклов ПЛК
        /// </summary>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public async Task StartMarkSingleLayer(int layerId)
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) запустил прожиг {layerId} слоя.");
            if (!await IsReadyToPrintLayer())
            {
                await _loggerService.InformationAsync(this, $"Запуск проожига {layerId} слоя отменен");
                await ShowErrorReasonForUser();
                await SetStateInternal(PrintServiceState.Pause);
                return;
            }
            if (ActiveProject == null)
            {
                await ShowErrorAsync("Проект не выбран");
                return;
            }
            var layer = ActiveProject.GetLayerById(layerId);
            if (layer == null)
            {
                await ShowErrorAsync("Слой не найден");
                return;
            }

            Mode = PrintServiceMode.Manual;
            _ = Task.Run(async () => await _multiScanatorSystemProxy.StartLayerMarkingAsync(layer));
            _eventAggregator.GetEvent<OnLayerPrintStart>().Publish(layer);
            await _loggerService.InformationAsync(this, $"Запуск проожига {layerId} слоя успешно запущен");
        }


        public async Task ApplyLayerPowder()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) запустил нанесение порошка");
            //await ScpSetLayerThickness(ActiveProject.CurrentLayer.Height);
            await ComPlatformDown();
            await _loggerService.InformationAsync(this, $"The command was sent to leave the platform");
            
            await WaitForPlatformDownSuccessfully();
            await _loggerService.InformationAsync(this, $"The platform is released");


            if (!await IsDosing2Completed())
            {
                await _loggerService.InformationAsync(this, $"!await IsDosing2Completed(): {!await IsDosing2Completed()}");

                _isDosingStarted = true;
                await ComPrintDosing();
                await _loggerService.InformationAsync(this, $"Send dosing command");

                await WaitForPrintDosingSuccessfully();
                await _loggerService.InformationAsync(this, $"Dosing end");
                _isDosingStarted = false;
            }
            else
            {
                _isDosingStarted = false;
            }

            await ComPrintLayer();
            await _loggerService.InformationAsync(this, $"ComPrintLayer started");

            await Task.Delay(500);
            await WaitForPrintLayerSuccessfully();
            await _loggerService.InformationAsync(this, $"ComPrintLayer finished");

            if (!await IsDosing2Completed())
            {
                await _loggerService.InformationAsync(this, $"!await IsDosing2Completed(): {!await IsDosing2Completed()}");

                _isDosingStarted = true;
                await ComPrintDosing();
                await _loggerService.InformationAsync(this, $"ComPrintDosing started");

                await WaitForPrintDosingSuccessfully();
                await _loggerService.InformationAsync(this, $"ComPrintDosing finished");
                _isDosingStarted = false;
            }
            else
            {
                _isDosingStarted = false;
            }
            await _loggerService.InformationAsync(this, $"Нанесение порошка завершено");
        }

        public async Task PrintCurrentLayer()
        {
            _eventAggregator.GetEvent<OnLayerPrintStart>().Publish(ActiveProject.CurrentLayer);
            await _loggerService.InformationAsync(this, $"Print layer {ActiveProject.CurrentLayer} starting...");

            try
            {
                if (!await IsReadyToPrintLayer())
                {
                    await _loggerService.InformationAsync(this, $"PrintCurrentLayer aborted. IsReadyToPrintLayer has false");
                    await ShowErrorReasonForUser();
                    await SetStateInternal(PrintServiceState.Pause);
                    return;
                }

                // Начинаем отслеживание нового слоя в сессии
                var layerNumber = ActiveProject.CurrentLayer?.Id ?? 0;
                await _printSessionService.StartLayerAsync(layerNumber);

                await SetStateInternal(PrintServiceState.Started);
                Application.Current.Dispatcher.InvokeAsync(()=> _printLayerStart = true);

                await ComSetIsMarking(false);
                await _loggerService.InformationAsync(this, $"ComSetIsMarking = false");

                await ScpSetLayerThickness(50);
                await _loggerService.InformationAsync(this, $"ScpSetLayerThickness = 50");

                await ComPlatformDown();
                await _loggerService.InformationAsync(this, "Отправили команду на отпуск платформы");
                await WaitForPlatformDownSuccessfully(cancellationToken: _abortPrintCancellationTokenSource.Token);
                await _loggerService.InformationAsync(this, "Платформа опущена");

                // Фиксируем: платформа опущена
                await _printSessionService.UpdateLayerPlatformDownAsync();

                if (!await IsDosing2Completed())
                {
                    await _loggerService.InformationAsync(this, $"!await IsDosing2Completed(): {!await IsDosing2Completed()}");

                    _isDosingStarted = true;
                    await ComPrintDosing();
                    await _loggerService.InformationAsync(this, $"Отправили команду на дозирование");


                    await WaitForPrintDosingSuccessfully(cancellationToken: _abortPrintCancellationTokenSource.Token);
                    await _loggerService.InformationAsync(this, $"Дозирование завершено");
                    _isDosingStarted = false;
                }
                else
                {
                    _isDosingStarted = false;
                }

                await ComPrintLayer();
                await _loggerService.InformationAsync(this, $"ComPrintLayer started");

                await Task.Delay(1000);
                await WaitForPrintLayerSuccessfully(cancellationToken: _abortPrintCancellationTokenSource.Token);
                await _loggerService.InformationAsync(this, $"ComPrintLayer finish");


                // Фиксируем: порошок нанесён
                await _printSessionService.UpdateLayerPowderAppliedAsync();

                _startMarkTask = Task.Run(async () =>
                {
                    await _loggerService.InformationAsync(this, $"Задача по запуску сканирования началась");
                    while (true)
                    {
                        if (_abortPrintCancellationTokenSource != null &&
                            _abortPrintCancellationTokenSource.Token.IsCancellationRequested) return;

                        //if (await IsRecouterInDoorState() || await IsRecouterInHomeState())
                        //{
                            await ComSetIsMarking(true);
                            // Фиксируем: сканирование начато
                            await _printSessionService.UpdateLayerMarkingStartedAsync();
                            await _multiScanatorSystemProxy.StartLayerMarkingAsync(ActiveProject.CurrentLayer);
                            break;
                                //..}
                        await Task.Delay(500);
                    }
                    await _loggerService.InformationAsync(this, $"Задача по запуску сканирования завершена");
                });

                if (!await IsDosing2Completed())
                {
                    _isDosingStarted = true;
                    await ComPrintDosing();
                    await WaitForPrintDosingSuccessfully(cancellationToken: _abortPrintCancellationTokenSource.Token);
                    _isDosingStarted = false;
                }
                else
                {
                    _isDosingStarted = false;
                }
                Application.Current.Dispatcher.InvokeAsync(() => _printLayerStart = false);
                Console.WriteLine("ЦИКЛЫ ПЛК ЗАКОНЧИЛИСЬ");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        /// <summary>
        /// Обновление состояния переменных, необходимых для запуска печати слоя
        /// </summary>
        /// <returns></returns>
        public async Task UpdateBeforeLayerPrintConditionVariables()
        {
            // 1. Готовность газовой системы
            _readyGasSystem = await _logicControllerProvider.GetBoolAsync(OpcCommands.readySystemGasForPrint);
            await _loggerService.InformationAsync(this, $"OpcCommands.readySystemGasForPrint = {_readyGasSystem}");

            // 2. Готовность лазерной системы
            _readyLaserSystem = await _logicControllerProvider.GetBoolAsync(OpcCommands.readySystemLaserForPrint);
            await _loggerService.InformationAsync(this, $"OpcCommands.readySystemLaserForPrint = {_readyLaserSystem}");


            // 3. Готовность нагревателя
            _readyHeatingTable = await _logicControllerProvider.GetBoolAsync(OpcCommands.readySistemHeatingTable);
            await _loggerService.InformationAsync(this, $"OpcCommands.readySistemHeatingTable = {_readyHeatingTable}");


            // 4. Отсутствие ошибок блокировки
            _notErrorsBlockingPrinting = await _logicControllerProvider.GetBoolAsync(OpcCommands.notErrorsBlockingPrinting);
            await _loggerService.InformationAsync(this, $"OpcCommands.notErrorsBlockingPrinting = {_notErrorsBlockingPrinting}");


            // 5. Ошибка реферирования рекоутера
            _recoaterRefError = await _logicControllerProvider.GetBoolAsync(OpcCommands.Err_Powder_RecoaterRef);
            await _loggerService.InformationAsync(this, $"OpcCommands.Err_Powder_RecoaterRef = {_recoaterRefError}");


            // 6. Ошибка привода рекоутера
            _recoaterEngineError = await _logicControllerProvider.GetBoolAsync(OpcCommands.Err_Axes_Recoater);
            await _loggerService.InformationAsync(this, $"OpcCommands.Err_Axes_Recoater = {_recoaterEngineError}");


            // 7. Ошибка привода дозатора
            _doserAxesError = await _logicControllerProvider.GetBoolAsync(OpcCommands.Err_Axes_Doser);
            await _loggerService.InformationAsync(this, $"OpcCommands.Err_Axes_Doser = {_doserAxesError}");

            // 8. Ошибка привода платформы
            _platformAxesError = await _logicControllerProvider.GetBoolAsync(OpcCommands.Err_Axes_Platform);
            await _loggerService.InformationAsync(this, $"OpcCommands.Err_Axes_Platform = {_platformAxesError}");

        }

        /// <summary>
        /// Последовательно проверяем каждую переменную и выводим ошибку пользователю если её значение нас не устраивает
        /// </summary>
        /// <returns></returns>
        public async Task ShowErrorReasonForUser()
        {
            // 0. Подключен ли ПЛК
            if (!_logicControllerProvider.Connected)
            {
                await ShowErrorAsync("Подключение к ПЛК отсутствует");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Подключение к ПЛК отсутствует");
                return;
            }

            _automaticProcessSettings = Bootstrapper.Configuration.Get<AutomaticProcessSettings>();

            // 1. Готовность газовой системы
            if (_automaticProcessSettings.ReadyGasSystemCheck && !_readyGasSystem)
            {
                await ShowErrorAsync("Система подачи газа не готова к работе");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Система подачи газа не готова к работе");
                return;
            }

            // 2. Готовность лазерной системы
            if (_automaticProcessSettings.ReadyLaserSystemCheck && !_readyLaserSystem)
            {
                await ShowErrorAsync("Лазерная система не готова к работе");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Лазерная система не готова к работе");
                return;
            }

            // 3. Готовность нагревателя
            if (_automaticProcessSettings.ReadyHeatingTableCheck && !_readyHeatingTable)
            {
                await ShowErrorAsync("Нагреватель не готов к работе");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Нагреватель не готов к работе");
                return;
            }

            // 4. Отсутствие ошибок блокировки
            if (_automaticProcessSettings.NotErrorsBlockingPrintingCheck && !_notErrorsBlockingPrinting)
            {
                await ShowErrorAsync("Имеются ошибки блокировки печати");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Имеются ошибки блокировки печати");
                return;
            }

            // 5. Ошибка реферирования рекоутера
            if (_automaticProcessSettings.RecoaterRefErrorCheck && _recoaterRefError)
            {
                await ShowErrorAsync("Ошибка реферирования рекоутера");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Ошибка реферирования рекоутера");
                return;
            }

            // 6. Ошибка привода рекоутера
            if (_automaticProcessSettings.RecoaterEngineErrorCheck && _recoaterEngineError)
            {
                await ShowErrorAsync("Ошибка привода рекоутера");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Ошибка привода рекоутера");
                return;
            }

            // 7. Ошибка привода дозатора
            if (_automaticProcessSettings.DoserAxesErrorCheck && _doserAxesError)
            {
                await ShowErrorAsync("Ошибка привода дозатора");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Ошибка привода дозатора");
                return;
            }

            // 8. Ошибка привода платформы
            if (_automaticProcessSettings.PlatformAxesErrorCheck && _platformAxesError)
            {
                await ShowErrorAsync("Ошибка привода платформы");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Ошибка привода платформы");

                return;
            }
        }


        /// <summary>
        /// Проверка всех переменных на готовность к печати
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsReadyToPrintLayer()
        {
            if (!_logicControllerProvider.Connected) return false;

            await UpdateBeforeLayerPrintConditionVariables();
            _automaticProcessSettings = Bootstrapper.Configuration.Get<AutomaticProcessSettings>();

            if (_automaticProcessSettings.ReadyGasSystemCheck && !_readyGasSystem) return false;
            if (_automaticProcessSettings.ReadyLaserSystemCheck && !_readyLaserSystem) return false;
            if (_automaticProcessSettings.ReadyHeatingTableCheck && !_readyHeatingTable) return false;
            if (_automaticProcessSettings.NotErrorsBlockingPrintingCheck && !_notErrorsBlockingPrinting) return false;
            if (_automaticProcessSettings.RecoaterRefErrorCheck && _recoaterRefError) return false;
            if (_automaticProcessSettings.RecoaterEngineErrorCheck && _recoaterEngineError) return false;
            if (_automaticProcessSettings.DoserAxesErrorCheck && _doserAxesError) return false;
            if (_automaticProcessSettings.PlatformAxesErrorCheck && _platformAxesError) return false;
            
            return true;
        }

        /// <summary>
        /// Запустить печать активного проекта
        /// </summary>
        /// <returns></returns>
        public async Task StartPrint()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) запустил печать проекта");
            await _loggerService.InformationAsync(this, $"StartPrint attempt start");

            // Если пользователь не выбрал проект в списке проектов
            if (ActiveProject == null)
            {
                await ShowErrorAsync("Не выбран проект для печати");
                await _loggerService.ErrorAsync(this, $"Ошибка печати: Не выбран проект для печати");
                return;
            }

            //Блок проверок для запуска печати первого слоя
            if (!await IsReadyToPrintLayer())
            {
                await _loggerService.ErrorAsync(this, $"Ошибка печати: IsReadyToPrintLayer = false");
                await ShowErrorReasonForUser();
                return;
            }

            _abortPrintCancellationTokenSource = new CancellationTokenSource();
            await _logicControllerProvider.SetUInt32Async(OpcCommands.SCP_SetLayerThickness, 50);

            // Создаём сессию печати
            await _printSessionService.StartSessionAsync(ActiveProject);

            _ = Task.Run(async () =>
            {
                ActiveProject.SetActiveLayer(ActiveProject.Layers.First());
                await PrintCurrentLayer();
            });
        }

        /// <summary>
        /// Поставить печать на паузу
        /// </summary>
        /// <returns></returns>
        public async Task Pause()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) поставил печать на паузу");
            await SetStateInternal(PrintServiceState.Pause);
            await CustomMessageBox.ShowInformationAsync("Изменение состояния печати", "Печать будет поставлена на паузу по окончанию текущего слоя");
        }

        /// <summary>
        /// Возобновить печать
        /// </summary>
        /// <returns></returns>
        public async Task Continue()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) возобновил печать проекта");
            ActiveProject.NextLayer();
            Task.Run(async()=> await PrintCurrentLayer());
        }

        /// <summary>
        /// Остановить печать
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) прервал печать проекта");

            await SetStateInternal(PrintServiceState.Stop);
            _abortPrintCancellationTokenSource.Cancel(false);
            await _multiScanatorSystemProxy.StopMark();

            // Останавливаем сессию (оператор нажал Стоп)
            await _printSessionService.StopSessionAsync();

            if (ActiveProject != null)
            {
                ActiveProject.CurrentLayer = ActiveProject.Layers.First();
            }
        }

        /// <summary>
        /// Остановить печать одного слоя
        /// </summary>
        /// <returns></returns>
        public async Task StopSingleLayer()
        {
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) прервал прожиг слоя");

            await SetStateInternal(PrintServiceState.Stop);
            await _multiScanatorSystemProxy.StopMark();
        }

        /// <summary>
        /// Возобновить печать с конкретного слоя (после прерывания)
        /// </summary>
        /// <param name="session">Прерванная сессия печати</param>
        /// <param name="project">Проект для печати</param>
        /// <returns></returns>
        public async Task ResumePrintFromSession(PrintSession session, Project project)
        {
            if (!await IsReadyToPrintLayer())
            {
                await ShowErrorReasonForUser();
                return;
            }

            if (project == null)
            {
                await ShowErrorAsync("Не удалось загрузить проект для возобновления печати");
                return;
            }

            if (session == null)
            {
                await ShowErrorAsync("Сессия печати не найдена");
                return;
            }

            // Устанавливаем активный проект
            ActiveProject = project;
            _eventAggregator.GetEvent<OnActiveProjectSelected>().Publish(project);

            // Вычисляем номер следующего слоя для печати
            int resumeLayerIndex = session.LastCompletedLayer + 1;

            if (resumeLayerIndex >= project.Layers.Count)
            {
                await CustomMessageBox.ShowWarningAsync(
                    "Невозможно возобновить",
                    $"Последний завершённый слой: {session.LastCompletedLayer + 1}.\n" +
                    $"Всего слоёв в проекте: {project.Layers.Count}.\n\n" +
                    $"Печать уже завершена или проект изменился."
                );
                return;
            }

            Console.WriteLine($"[PrintService] Resuming print from layer {resumeLayerIndex} " +
                              $"(completed: {session.LastCompletedLayer + 1}/{project.Layers.Count})");

            // Восстанавливаем сессию как текущую
            _printSessionService.CurrentSession = session;

            _abortPrintCancellationTokenSource = new CancellationTokenSource();
            await _logicControllerProvider.SetUInt32Async(OpcCommands.SCP_SetLayerThickness, 50);

            Console.WriteLine($"Последний выполненный слой: {session.LastCompletedLayer}");
            var layerState = await _printSessionService.GetLastLayerBySessionIdAndLayerNumber(session, session.LastCompletedLayer + 1);
            if (layerState != null)
            {
                await SetStateInternal(PrintServiceState.Started);
                ActiveProject.SetActiveLayer(ActiveProject.GetLayerById(layerState.LayerNumber));
                _ = Task.Run(async () =>
                {
                    if (!layerState.IsPlatformDown)
                    {
                        await ComPlatformDown();
                        await WaitForPlatformDownSuccessfully(cancellationToken: _abortPrintCancellationTokenSource.Token);
                    }
                    if (!layerState.IsPowderApplied)
                    {
                        if (!await IsDosing2Completed())
                        {
                            _isDosingStarted = true;
                            await ComPrintDosing();
                            Console.WriteLine("Отправили команду на дозирование");
                            await WaitForPrintDosingSuccessfully();
                            Console.WriteLine("Дозирование завершено");
                            _isDosingStarted = false;
                        }
                        else
                        {
                            _isDosingStarted = false;
                        }

                        await ComPrintLayer();
                        await Task.Delay(500);
                        await WaitForPrintLayerSuccessfully();

                        if (!await IsDosing2Completed())
                        {
                            _isDosingStarted = true;
                            await ComPrintDosing();
                            await WaitForPrintDosingSuccessfully();
                            _isDosingStarted = false;
                        }
                        else
                        {
                            _isDosingStarted = false;
                        }
                        Console.WriteLine("Нанесение порошка завершено");
                    }
                    if (!layerState.IsMarkingFinished)
                    {
                        _startMarkTask = Task.Run(async () =>
                        {
                            Console.WriteLine("Задача по запуску сканирования началась");
                            while (true)
                            {
                                if (_abortPrintCancellationTokenSource != null &&
                                    _abortPrintCancellationTokenSource.Token.IsCancellationRequested) return;

                                //if (await IsRecouterInDoorState() || await IsRecouterInHomeState())
                                //{
                                await ComSetIsMarking(true);
                                // Фиксируем: сканирование начато
                                await _printSessionService.UpdateLayerMarkingStartedAsync();
                                await _multiScanatorSystemProxy.StartLayerMarkingAsync(ActiveProject.CurrentLayer);
                                break;
                                //..}
                                await Task.Delay(500);
                            }
                            Console.WriteLine("Задача по запуску сканирования завершена");
                        });
                    }

                    // Устанавливаем текущий слой на тот, с которого нужно возобновить
                });

                await CustomMessageBox.ShowSuccessAsync(
                    "Печать возобновлена",
                    $"Печать проекта \"{project.ProjectInfo?.Name}\" возобновлена со слоя {resumeLayerIndex + 1}."
                );
            }
        }

        /// <summary>
        /// Установить активный проект для печати
        /// </summary>
        /// <param name="project"></param>
        public async void SetActiveProject(Project project)
        {
            ActiveProject = project;
            await _loggerService.InformationAsync(this, $"{_authorizationService.GetUser().Login} ({_authorizationService.GetUser().Name} {_authorizationService.GetUser().Family}) установил активный проект {ActiveProject.ProjectInfo.Name}");
            _eventAggregator.GetEvent<OnActiveProjectSelected>().Publish(project);
        }

        /// <summary>
        /// Получить прогресс печати
        /// </summary>
        /// <returns></returns>
        public double GetLayerProgress()
        {
            return _multiScanatorSystemProxy.GetLayerProgress();
        }

        /// <summary>
        /// Получить прогресс первого сканатора (227, LaserNum=1)
        /// </summary>
        public double GetScanner1Progress()
        {
            return _multiScanatorSystemProxy.GetScanner1Progress();
        }

        /// <summary>
        /// Получить прогресс второго сканатора (228, LaserNum=0)
        /// </summary>
        public double GetScanner2Progress()
        {
            return _multiScanatorSystemProxy.GetScanner2Progress();
        }

        /// <summary>
        /// Получить текущий режим маркировки
        /// </summary>
        public MultiMarkingState GetMarkingState()
        {
            return _multiScanatorSystemProxy.GetMarkingState();
        }

        /// <summary>
        /// Получить ID сканатора, который маркирует в режиме Single
        /// </summary>
        public int GetSingleMarkingScanatorId()
        {
            return _multiScanatorSystemProxy.GetSingleMarkingScanatorId();
        }

        /// <summary>
        /// Отобразить пользователю ошибку
        /// </summary>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public async Task ShowErrorAsync(string errorDescription)
        {
            await _notificationService.AddErrorAsync("Ошибка", errorDescription);
            await CustomMessageBox.ShowErrorAsync("Ошибка", errorDescription);
        }

        /// <summary>
        /// Изменение состояния сервиса
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task SetStateInternal(PrintServiceState state)
        {
            State = state;
            _eventAggregator.GetEvent<OnPrintServiceStateChangedEvent>().Publish(state);
        }

        /// <summary>
        /// Получить текущий статус сервиса печати
        /// </summary>
        /// <returns></returns>
        public async Task<PrintServiceState> GetState() => await Task.FromResult(State);
        public async Task<bool> IsDosing1Completed() => await _logicControllerProvider.GetBoolAsync(OpcCommands.dosing1Completed);
        public async Task<bool> IsDosing2Completed() => await _logicControllerProvider.GetBoolAsync(OpcCommands.dosing2Completed);
        public async Task ComPrintDosing() => await _logicControllerProvider.SetBoolAsync(OpcCommands.Print_Dosing, true);
        public async Task WaitForPrintDosingSuccessfully(int delay = 100, CancellationToken? cancellationToken = null) => await _logicControllerProvider.WaitBoolValue(OpcCommands.dosingSuccess, true, delay, cancellationToken);
        public async Task<bool> DosingIsSuccess() => await _logicControllerProvider.GetBoolAsync(OpcCommands.dosingSuccess);
        public async Task ComPrintLayer()
        {
            Console.WriteLine($"ComPrintLayer");
            await _logicControllerProvider.SetBoolAsync(OpcCommands.Print_Layer, true);
        }

        public async Task WaitForPrintLayerSuccessfully(int delay = 100, CancellationToken? cancellationToken = null) => await _logicControllerProvider.WaitBoolValue(OpcCommands.layerSuccess, true, delay, cancellationToken); 
        public async Task IsLayerSuccessfully() => await _logicControllerProvider.GetBoolAsync(OpcCommands.layerSuccess);
        public async Task<bool> IsRecouterInDoorState() => await _logicControllerProvider.GetBoolAsync(OpcCommands.RecouterInDoorState);
        public async Task<bool> IsRecouterInHomeState() => await _logicControllerProvider.GetBoolAsync(OpcCommands.RecouterInHomeState);
        private bool IsAborted() => _abortPrintCancellationTokenSource != null &&
                                    _abortPrintCancellationTokenSource.Token.IsCancellationRequested;
        private async Task ComSetIsMarking(bool state) => await _logicControllerProvider.SetBoolAsync(OpcCommands.IsMarking, state);
        private async Task ComPlatformDown() => await _logicControllerProvider.SetBoolAsync(OpcCommands.Print_PlatformDown, true);
        private async Task WaitForPlatformDownSuccessfully(int delay = 500, CancellationToken? cancellationToken = null) => await Task.Delay(500);
        private async Task ScpSetLayerThickness(uint micron) => await _logicControllerProvider.SetUInt32Async(OpcCommands.SCP_SetLayerThickness, micron);
        public bool IsScanBoardsReady() => _multiScanatorSystemProxy != null && _multiScanatorSystemProxy.IsBoardsConnected();
    }
}
