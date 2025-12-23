using Emgu.CV;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using LayerAnalyzer.Lib.Models.Calibration;
using LayerAnalyzer.Lib.Services.Utils;

namespace PrintMate.Terminal.Services;

public class CameraService
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private int _selectedCameraIndex;
    private List<string> _availableCameras;
    private VideoCapture _videoCapture;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _captureTask;
    private readonly IEventAggregator _eventAggregator;
    private readonly ConfigurationManager _configurationManager;

    private CalibrationSettings _calibrationSettings;
    private string _calibrationPath;

    private bool _isStarted;
    private bool _isStarting;
    private readonly object _startLock = new();

    public event Action<BitmapSource> OnUpdated;
    public event Action<bool> OnLoadingStateChanged;

    public CameraService(IEventAggregator eventAggregator, ConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
        _eventAggregator = eventAggregator;
        _selectedCameraIndex = -1;
        _availableCameras = [];

        _eventAggregator.GetEvent<OnCameraSelectedEvent>().Subscribe(OnCameraSelected);
        _calibrationPath = _configurationManager.Get<LayerAnalyzerSettings>().CalibrationSettingsPath;
        // Не запускаем камеру сразу - ленивый запуск
    }

    private void OnCameraSelected(CameraItem obj)
    {
        _selectedCameraIndex = obj.Id;
        _isStarted = false; // Сбрасываем флаг, чтобы перезапустить камеру
        _ = StartCameraAsync();
    }

    /// <summary>
    /// Публичный метод для ленивого запуска камеры (вызывается при показе UI)
    /// </summary>
    public Task EnsureStartedAsync()
    {
        if (_isStarted && _videoCapture?.IsOpened == true)
        {
            return Task.CompletedTask;
        }
        return StartCameraAsync();
    }

    /// <summary>
    /// Асинхронно получает список доступных камер
    /// </summary>
    private static Task<List<string>> GetAvailableCamerasAsync()
    {
        return Task.Run(() =>
        {
            var cameras = new List<string>();
            int index = 0;

            // Пробуем открыть камеры с индексами от 0 до 10
            while (index < 10)
            {
                using (var capture = new VideoCapture(index))
                {
                    if (capture.IsOpened)
                    {
                        cameras.Add($"Camera {index}");
                    }
                    else
                    {
                        break; // Если камера не открылась, прекращаем поиск
                    }
                }
                index++;
            }

            return cameras;
        });
    }

    private async Task StartCameraAsync()
    {
        // Предотвращаем одновременный запуск из нескольких потоков
        lock (_startLock)
        {
            if (_isStarting)
            {
                return;
            }
            _isStarting = true;
        }

        try
        {
            // Оповещаем UI о начале загрузки
            Application.Current.Dispatcher.Invoke(() => OnLoadingStateChanged?.Invoke(true));

            // Загружаем калибровку асинхронно (если еще не загружена)
            if (_calibrationSettings == null)
            {
                await Task.Run(() => LoadSettings());
            }

            StopCamera();

            // Асинхронный поиск камер
            _availableCameras = await GetAvailableCamerasAsync();

            if (_availableCameras.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Камеры не обнаружены!");
                });
                return;
            }

            // Попытка использовать сохранённый индекс или загрузить из конфигурации
            int cameraIndex = _selectedCameraIndex;

            if (cameraIndex < 0)
            {
                var settings = _configurationManager.Get<CameraSettings>();
                cameraIndex = settings.SelectedCameraIndex;
            }

            // Проверяем, что индекс в допустимых пределах
            if (cameraIndex >= _availableCameras.Count)
            {
                cameraIndex = 0;
            }

            _selectedCameraIndex = cameraIndex;

            // Создаём VideoCapture асинхронно
            _videoCapture = await Task.Run(() =>
            {
                var capture = new VideoCapture(cameraIndex);
                if (capture.IsOpened)
                {
                    // Устанавливаем разрешение 2048x1536
                    capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 2048);
                    capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 1536);
                }
                return capture;
            });

            if (!_videoCapture.IsOpened)
            {
                throw new Exception($"Не удалось открыть камеру с индексом {cameraIndex}");
            }

            // Запускаем асинхронный захват кадров
            _cancellationTokenSource = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureFramesAsync(_cancellationTokenSource.Token));

            _isStarted = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Ошибка при запуске камеры: {ex}");
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка камеры", $"Не удалось запустить камеру:\n{ex.Message}");
            });
        }
        finally
        {
            _isStarting = false;
            // Оповещаем UI о завершении загрузки
            Application.Current.Dispatcher.Invoke(() => OnLoadingStateChanged?.Invoke(false));
        }
    }

    private async Task CaptureFramesAsync(CancellationToken cancellationToken)
    {
        var frame = new Mat();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_videoCapture != null && _videoCapture.IsOpened)
                {
                    // Захватываем кадр
                    _videoCapture.Read(frame);

                    if (!frame.IsEmpty)
                    {
                        if (_calibrationSettings != null)
                        {
                            frame = _calibrationSettings.ApplyToFrame(frame);
                        }

                        // Конвертируем Mat в BitmapSource
                        var bitmapSource = ConvertMatToBitmapSource(frame);

                        // Отправляем кадр в UI поток
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OnUpdated?.Invoke(bitmapSource);
                        });
                    }
                }

                // Задержка ~16ms для ~60 FPS
                await Task.Delay(50, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Ошибка при захвате кадров: {ex}");
        }
        finally
        {
            frame?.Dispose();
        }
    }

    /// <summary>
    /// Конвертирует Mat из Emgu.CV в WPF BitmapSource
    /// </summary>
    private static BitmapSource ConvertMatToBitmapSource(Mat mat)
    {
        try
        {
            // Конвертируем Mat в Bitmap через Emgu.CV.Bitmap
            using (var bitmap = mat.ToBitmap())
            {
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    bitmapSource.Freeze(); // Делаем BitmapSource потокобезопасным
                    return bitmapSource;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Ошибка при конвертации кадра: {ex}");
            return null;
        }
    }

    private void LoadSettings()
    {
        try
        {
            _calibrationSettings = new CalibrationSettingsLoader().Load(_calibrationPath);
        }
        catch (Exception ex)
        {
            _calibrationSettings = null;
            Console.WriteLine($"[CameraService] Ошибка при загрузке калибровочного файла: {ex}");
        }
    }

    private void StopCamera()
    {
        try
        {
            // Останавливаем асинхронный захват
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _captureTask?.Wait(1000); // Ждём максимум 1 секунду
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            // Освобождаем VideoCapture
            _videoCapture?.Dispose();
            _videoCapture = null;

            // Оповещаем подписчиков, что изображение больше не поступает
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnUpdated?.Invoke(null);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Ошибка при остановке камеры: {ex}");
        }
    }

    /// <summary>
    /// Освобождает ресурсы сервиса камеры
    /// </summary>
    public void Dispose()
    {
        StopCamera();
        _eventAggregator.GetEvent<OnCameraSelectedEvent>().Unsubscribe(OnCameraSelected);
    }
}