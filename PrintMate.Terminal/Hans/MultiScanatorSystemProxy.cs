using HansScannerHost.Models.Events;
using HansHostProvider.Shared;
using ImTools;
using Prism.Events;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Hans.NET.libs;
using Hans.NET.Models;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Hans;
using PrintMate.Terminal.Services;
using ProjectParserTest.Parsers.Shared.Enums;

namespace HansScannerHost.Models
{
    public enum MultiMarkingState
    {
        None,
        Single,
        Multi
    }


    /// <summary>
    /// Управление множественными Hans сканаторами через Named Pipe прокси
    /// Каждый сканатор работает в изолированном процессе HansScannerHost
    /// </summary>
    public class MultiScanatorSystemProxy : IDisposable
    {
        #region Singleton

        public static MultiScanatorSystemProxy? Instance { get; private set; }

        #endregion

        private readonly IEventAggregator _eventAggregator;
        private readonly LoggerService _loggerService;

        public readonly List<ScanatorProxyClient> Clients = new();
        public CliProvider CliProvider = new CliProvider();
        public MultiMarkingState MultiMarkingState = MultiMarkingState.None;

        private int _singleMarkingScanatorId = 0;

        public MultiScanatorSystemProxy(IEventAggregator eventAggregator, LoggerService loggerService)
        {
            _loggerService = loggerService;
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            Instance = this;
            Console.WriteLine("MultiScanatorSystemProxy initialized");

            // Создаём директорию для файлов
            if (!Directory.Exists("ScanAPI"))
            {
                Directory.CreateDirectory("ScanAPI");
            }
        }

        public async void CreateProxy(ScanatorProxyClient client)
        {
            await _loggerService.InformationAsync(this,
                $"Proxy successufull registered. Address: {await client.GetHostAddressAsync()}");
            Clients.Add(client);
        }

        /// <summary>
        /// Маркировка слоя с использованием обоих сканаторов
        /// </summary>
        public async Task<bool> StartLayerMarkingAsync(Layer layer)
        {
            try
            {
                if (Clients.Count < 2)
                {
                    throw new Exception("Need at least 2 scanners");
                }

                layer.Regions.RemoveAll(p => p.GeometryRegion == GeometryRegion.DownskinRegionPreview ||
                p.GeometryRegion == GeometryRegion.UpskinRegionPreview ||
                p.GeometryRegion == GeometryRegion.InfillRegionPreview);

                // 227 передний
                Layer firstLaserLayerCopy = RegionSlicer.GetLayerWithLaserRegionsById(layer, 1);

                // 228 задний
                Layer secondLaserLayerCopy = RegionSlicer.GetLayerWithLaserRegionsById(layer, 0);

                Console.WriteLine($"Количество регионов для 227: {firstLaserLayerCopy.Regions.Count}");
                Console.WriteLine($"Количество регионов для 228: {firstLaserLayerCopy.Regions.Count}");

                if (firstLaserLayerCopy.Regions.Count > 0 && secondLaserLayerCopy.Regions.Count > 0)
                {
                    Console.WriteLine("Включен режим мульти сканирования");
                    MultiMarkingState = MultiMarkingState.Multi;
                    
                    // Если включен режим мульти-сканирования, то принимаем события прогресса от обоих сканаторов
                }
                else
                {
                    Console.WriteLine("Включен режим одиночного сканирования");
                    MultiMarkingState = MultiMarkingState.Single;

                    // Теперь нужно определить какой именно сканатор будет маркировать
                    // Если печатает только один сканатор, то принимаем события прогресса только от него. 

                    if (firstLaserLayerCopy.Regions.Count > 0) _singleMarkingScanatorId = 0;
                    if (secondLaserLayerCopy.Regions.Count > 0) _singleMarkingScanatorId = 1;

                }

                await StartMultiLaserPrint(firstLaserLayerCopy, secondLaserLayerCopy);

                // Маркировка завершена
                _eventAggregator.GetEvent<OnLayerMarkFinish>().Publish(layer);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartLayerMarking error: {ex}");
                throw;
            }
        }

        public async Task StartSingleLaserPrint(Layer layer)
        {
            var scannerProxy = Clients[0];

            string udmFile = scannerProxy.UdmBuilder.BuildLayer(layer);
            bool result = await scannerProxy.DownloadMarkFileAsync(udmFile);
            if (result)
            {
                Console.WriteLine("[Single-Mode] Download started");
            }

            while (true)
            {
                if (scannerProxy.IsDownloadFinish)
                {
                    break;
                }
                await Task.Delay(100);
            }

            await scannerProxy.StartMarkAsync();

            while (true)
            {
                if (scannerProxy.IsMarkComplete)
                {
                    break;
                }
                await Task.Delay(100);
            }
        }

        public async Task StartMultiLaserPrint(Layer layer1, Layer layer2)
        {
            await _loggerService.InformationAsync(this,
                $"StartMultiLaserPrint. Layer1 regions count: {layer1.Regions.Count}, Layer2 regions count: {layer2.Regions.Count}");

            var scannerProxy1 = Clients[0];
            var scannerProxy2 = Clients[1];

            bool download1Result = await scannerProxy1.DownloadMarkFileAsync(scannerProxy1.UdmBuilder.BuildLayer(layer1));
            await Task.Delay(100);
            bool download2Result = await scannerProxy2.DownloadMarkFileAsync(scannerProxy2.UdmBuilder.BuildLayer(layer2));
            
            if (download1Result && download2Result)
            {
                Console.WriteLine("[Multi-Mode] Download started");
                await _loggerService.InformationAsync(this, $"StartMultiLaserPrint: Download started");
            }

            while (true)
            {
                if (scannerProxy1.IsDownloadFinish && scannerProxy2.IsDownloadFinish)
                {
                    await _loggerService.InformationAsync(this, $"StartMultiLaserPrint: Download finished");
                    break;
                } 
                await Task.Delay(100);
            }

            await scannerProxy1.StartMarkAsync();
            await scannerProxy2.StartMarkAsync();
            await _loggerService.InformationAsync(this, $"StartMultiLaserPrint: Start marking");

            while (true)
            {
                if (scannerProxy1.IsMarkComplete && scannerProxy2.IsMarkComplete)
                {
                    await _loggerService.InformationAsync(this, $"StartMultiLaserPrint: Marking complete");
                    break;
                }
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Получить статус всех сканаторов
        /// </summary>
        public async Task<List<HansHostProvider.Shared.ScanatorStatus?>> GetAllStatusesAsync()
        {
            var tasks = Clients.Select(client => client.GetStatusAsync()).ToArray();
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task StopMark()
        {
            await GetScanner(0)!.StopMarkAsync();
            await GetScanner(1)!.StopMarkAsync();
        }

        public double GetLayerProgress()
        {
            double progress = 0;

            if (MultiMarkingState == MultiMarkingState.Single)
            {
                var singleScanner = GetScanner(_singleMarkingScanatorId);
                if (singleScanner != null)
                {
                    progress = singleScanner.MarkProgress;
                }
            }
            else
            {
                var firstScanner = GetScanner(0);
                var secondScanner = GetScanner(1);

                if (firstScanner != null && secondScanner != null)
                {
                    int current = firstScanner.MarkProgress + secondScanner.MarkProgress;
                    progress = (current / 200d) * 100;
                    progress = Math.Max(0, Math.Min(100, progress));
                }
            }
            return progress;
        }

        /// <summary>
        /// Получить прогресс первого сканатора (227, LaserNum=1)
        /// Если сканатор завершил маркировку, возвращает 100%
        /// </summary>
        public double GetScanner1Progress()
        {
            var scanner = GetScanner(0);
            if (scanner == null) return 0;

            // Если сканатор завершил маркировку, всегда возвращаем 100%
            if (scanner.IsMarkComplete) return 100;

            return scanner.MarkProgress;
        }

        /// <summary>
        /// Получить прогресс второго сканатора (228, LaserNum=0)
        /// Если сканатор завершил маркировку, возвращает 100%
        /// </summary>
        public double GetScanner2Progress()
        {
            var scanner = GetScanner(1);
            if (scanner == null) return 0;

            // Если сканатор завершил маркировку, всегда возвращаем 100%
            if (scanner.IsMarkComplete) return 100;

            return scanner.MarkProgress;
        }

        /// <summary>
        /// Получить текущий режим маркировки (Single или Multi)
        /// </summary>
        public MultiMarkingState GetMarkingState()
        {
            return MultiMarkingState;
        }

        /// <summary>
        /// Получить ID сканатора, который маркирует в режиме Single
        /// </summary>
        public int GetSingleMarkingScanatorId()
        {
            return _singleMarkingScanatorId;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing MultiScanatorSystemProxy...");

            // Закрываем все прокси-клиенты (это также завершит хост-процессы)
            foreach (var client in Clients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing client: {ex.Message}");
                }
            }

            Clients.Clear();
            Console.WriteLine("MultiScanatorSystemProxy disposed");
        }

        public ScanatorProxyClient? GetScanner(int id)
        {
            return Clients?.ElementAtOrDefault(id);
        }

        public bool IsBoardsConnected()
        {
            if (Clients.Count < 2) return false;
            return (Clients[0].ConnectState == ConnectState.Connected &&
                    Clients[1].ConnectState == ConnectState.Connected);
        }
    }
}
