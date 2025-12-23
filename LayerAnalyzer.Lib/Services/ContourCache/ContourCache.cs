using System.Drawing;
using System.Text.Json;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;

namespace LayerAnalyzer.Lib.Services.ContourCache;

/// <summary>
/// Кэш контуров с поддержкой файлового хранилища
/// </summary>
public class ContourCache : IContourCache
{
    private readonly int _sizeBuffer;
    private readonly LinkedList<Dictionary<DefectType, VectorOfVectorOfPoint>> _buffer;
    private readonly string _cacheFolder; // Папка для файлового кэша
    private int _rangeStart;
    private int _rangeEnd;
    private bool _isLastReadOperation = false;

    public ContourCache(int sizeBuffer, int curLayer, string cacheFolder)
    {
        _sizeBuffer = sizeBuffer;
        _cacheFolder = cacheFolder;
        _buffer = new LinkedList<Dictionary<DefectType, VectorOfVectorOfPoint>>();
        _rangeStart = curLayer;
        _rangeEnd = curLayer + 1;

        // Создаём папку для кэша, если её нет
        if (!Directory.Exists(_cacheFolder))
        {
            Directory.CreateDirectory(_cacheFolder);
        }
    }

    public ContourCache(int sizeBuffer, string cacheFolder) : this(sizeBuffer, 0, cacheFolder)
    {
    }

    public void SetCurLayer(int curLayer)
    {
        if (curLayer < 0)
            throw new ArgumentException("Layer number cannot be negative");

        _rangeStart = curLayer;
        _rangeEnd = curLayer + 1;
    }

    public int GetCurLayer()
    {
        return _rangeEnd - 1;
    }

    /// <summary>
    /// Перезагрузить буфер из файлов кэша
    /// </summary>
    public void Reload()
    {
        _buffer.Clear();
        _isLastReadOperation = true; // После очистки буфера, последней операцией считается чтение

        // Загружаем последние sizeBuffer слоёв в буфер, начиная с _rangeEnd - 1 и двигаясь назад
        int layersToLoad = Math.Min(_sizeBuffer, _rangeEnd);

        for (int i = 0; i < layersToLoad; i++)
        {
            int layerNum = _rangeEnd - 1 - i; // Текущий слой для загрузки
            if (layerNum < 0)
                break;

            try
            {
                var cacheLayer = ReadCache(layerNum);
                if (cacheLayer != null)
                {
                    _buffer.AddFirst(cacheLayer); // Добавляем в начало буфера
                    _rangeStart = layerNum; // Устанавливаем start на первый загруженный слой
                }
                else
                {
                    // Если файла нет, останавливаем загрузку
                    break;
                }
            }
            catch (IOException)
            {
                // Если ошибка при чтении, останавливаем загрузку
                break;
            }
        }
    }


    public List<Dictionary<DefectType, VectorOfVectorOfPoint>> Get()
    {
        return GetRange(_rangeStart, _rangeEnd);
    }

    public List<Dictionary<DefectType, VectorOfVectorOfPoint>> GetLayers(int numLayer, int size)
    {
        int start = numLayer - size + 1;
        int end = numLayer + 1;
        return GetRange(start, end);
    }

    public List<Dictionary<DefectType, VectorOfVectorOfPoint>> GetRange(int start, int end)
    {
        int layerRangeSize = end - start;

        if (layerRangeSize < 0)
            return new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();

        // Если запрашиваемый диапазон больше буфера, обрезаем его
        if (layerRangeSize > (_rangeEnd - _rangeStart))
        {
            start = end - (_rangeEnd - _rangeStart);
        }

        // Пытаемся получить из буфера
        var resultFromBuffer = TryGetFromBuffer(start, end);
        if (resultFromBuffer.Count > 0)
        {
            return resultFromBuffer;
        }

        // Пытаемся получить из файлов
        // Обновляем файловый список перед доступом
        if (!_isLastReadOperation)
        {
            _isLastReadOperation = true;
        }
        return TryGetFromFolder(start, end);
    }

    private List<Dictionary<DefectType, VectorOfVectorOfPoint>> TryGetFromBuffer(int start, int end)
    {
        // Проверяем, что запрашиваемый диапазон находится в буфере
        if (end <= _rangeEnd && start >= _rangeStart)
        {
            var result = new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();
            int startIndex = start - _rangeStart;
            int layerRangeSize = end - start;

            // Копируем данные из буфера
            int index = 0;
            foreach (var item in _buffer)
            {
                if (index >= startIndex && index < startIndex + layerRangeSize)
                {
                    result.Add(item);
                }
                index++;
            }

            return result;
        }

        return new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();
    }

    private List<Dictionary<DefectType, VectorOfVectorOfPoint>> TryGetFromFolder(int start, int end)
    {
        var result = new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();

        // Обновляем флаг, так как собираемся читать
        _isLastReadOperation = true;

        // Исправленный цикл: от start до end (не start + size)
        for (int layerNum = start; layerNum < end; layerNum++)
        {
            // Если слой в буфере, берём из буфера
            if (layerNum >= _rangeStart && layerNum < _rangeEnd)
            {
                int layerIndexInBuffer = layerNum - _rangeStart;
                int index = 0;
                foreach (var item in _buffer)
                {
                    if (index == layerIndexInBuffer)
                    {
                        result.Add(item);
                        break;
                    }
                    index++;
                }
            }
            else
            {
                // Иначе читаем из файла
                try
                {
                    var cacheLayer = ReadCache(layerNum);
                    if (cacheLayer != null)
                    {
                        result.Add(cacheLayer);
                    }
                    else
                    {
                        // Если один из слоёв не найден, возвращаем пустой список
                        return new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();
                    }
                }
                catch (IOException)
                {
                    // Если ошибка при чтении, возвращаем пустой список
                    return new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();
                }
            }
        }

        return result;
    }

    public void Add(Dictionary<DefectType, VectorOfVectorOfPoint> contours)
    {
        int layerNum;

        // Проверяем, нужно ли расширить диапазон перед добавлением нового элемента.
        if (_buffer.Count == (_rangeEnd - _rangeStart))
        {
            layerNum = _rangeEnd;
            WriteCache(contours, layerNum);
            _rangeEnd++;
        }
        else
        {
            layerNum = _rangeEnd - 1;
            WriteCache(contours, layerNum);
        }

        // Добавляем контуры в конец буфера
        _buffer.AddLast(contours);

        // Удаляем старые элементы, если превышен размер буфера
        while (_buffer.Count > _sizeBuffer)
        {
            var removed = _buffer.First!.Value;
            _buffer.RemoveFirst();
            ReleaseMats(removed);
            _rangeStart++;
        }
    }

    /// <summary>
    /// Загрузить следующий слой из кэша
    /// </summary>
    public Dictionary<DefectType, VectorOfVectorOfPoint>? LoadNext()
    {
        // Обновляем флаг, так как собираемся читать
        if (!_isLastReadOperation)
        {
            _isLastReadOperation = true;
            // Здесь можно обновить список файлов, если используется кеширование
        }

        try
        {
            // Проверяем файл для следующего слоя *вне* текущего диапазона
            var next = ReadCache(_rangeEnd);
            if (next != null)
            {
                // Только если чтение успешно, добавляем в буфер...
                _buffer.AddLast(next);
                // ...и расширяем диапазон
                _rangeEnd++;

                // Удаляем старые элементы, если превышен размер буфера
                while (_buffer.Count > _sizeBuffer)
                {
                    var removed = _buffer.First!.Value;
                    _buffer.RemoveFirst();
                    ReleaseMats(removed);
                    _rangeStart++;
                }

                return next;
            }
        }
        catch (IOException)
        {
            return null;
        }
        return null;
    }

    public void Clear()
    {
        _buffer.Clear();
        _rangeStart = _rangeEnd;
    }

    public bool HasInCache(int curLayer)
    {
        // Проверяем буфер
        if (_rangeStart <= curLayer && curLayer < _rangeEnd)
        {
            return true;
        }

        // Проверяем файлы
        // Обновляем флаг, так как собираемся читать
        if (!_isLastReadOperation)
        {
            _isLastReadOperation = true;
            // Здесь можно обновить список файлов, если используется кеширование
        }
        string cacheFile = GetCacheFileName(curLayer);
        return File.Exists(cacheFile);
    }

    private void ReleaseMats(Dictionary<DefectType, VectorOfVectorOfPoint> removed)
    {
        foreach (var contours in removed.Values)
        {
            contours?.Dispose();
        }
    }

    /// <summary>
    /// Записать контуры в файл кэша
    /// </summary>
    private void WriteCache(Dictionary<DefectType, VectorOfVectorOfPoint> cachedLayer, int numLayer)
    {
        _isLastReadOperation = false;

        var cacheData = new Dictionary<string, List<List<Point>>>();

        foreach (var (defectType, contours) in cachedLayer)
        {
            var contoursList = new List<List<Point>>();

            for (int i = 0; i < contours.Size; i++)
            {
                using var contour = contours[i];
                var points = new List<Point>();

                for (int j = 0; j < contour.Size; j++)
                {
                    points.Add(new Point(contour[j].X, contour[j].Y));
                }

                contoursList.Add(points);
            }

            cacheData[defectType.ToString()] = contoursList;
        }

        string cacheFile = GetCacheFileName(numLayer);
        string json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cacheFile, json);
    }

    /// <summary>
    /// Прочитать контуры из файла кэша
    /// </summary>
    private Dictionary<DefectType, VectorOfVectorOfPoint>? ReadCache(int numLayer)
    {
        _isLastReadOperation = true;

        string cacheFile = GetCacheFileName(numLayer);
        if (!File.Exists(cacheFile))
        {
            return null;
        }

        string json = File.ReadAllText(cacheFile);
        Dictionary<string, List<List<Point>>> cacheData;
        try
        {
            cacheData = JsonSerializer.Deserialize<Dictionary<string, List<List<Point>>>>(json);
        }
        catch (JsonException)
        {
            // Если JSON повреждён, возвращаем null
            return null;
        }


        if (cacheData == null)
        {
            return null;
        }

        var result = new Dictionary<DefectType, VectorOfVectorOfPoint>();

        foreach (DefectType defectType in Enum.GetValues<DefectType>())
        {
            string key = defectType.ToString();
            if (cacheData.ContainsKey(key))
            {
                var contoursList = cacheData[key];
                var contours = new VectorOfVectorOfPoint();

                foreach (var points in contoursList)
                {
                    var pointsArray = points.Select(p => new Point(p.X, p.Y)).ToArray();
                    contours.Push(new VectorOfPoint(pointsArray));
                }

                result[defectType] = contours;
            }
        }

        return result;
    }

    private string GetCacheFileName(int numLayer)
    {
        return Path.Combine(_cacheFolder, $"layer_{numLayer:D6}.json");
    }
}