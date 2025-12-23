using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;

namespace LayerAnalyzer.Lib.Services.ContourCache;

/// <summary>
/// Кэш контуров в оперативной памяти с ограниченным буфером
/// </summary>
public class CacheInRam : IContourCache
{
    private readonly int _sizeBuffer;
    private readonly LinkedList<Dictionary<DefectType, VectorOfVectorOfPoint>> _buffer;
    private int _rangeStart;
    private int _rangeEnd;

    public CacheInRam(int sizeBuffer, int curLayer)
    {
        _sizeBuffer = sizeBuffer;
        _buffer = new LinkedList<Dictionary<DefectType, VectorOfVectorOfPoint>>();
        _rangeStart = curLayer;
        _rangeEnd = curLayer + 1;
    }

    public CacheInRam(int sizeBuffer) : this(sizeBuffer, 0)
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

        // Проверяем, что запрашиваемый диапазон находится в буфере
        if (end <= _rangeEnd && start >= _rangeStart)
        {
            var result = new List<Dictionary<DefectType, VectorOfVectorOfPoint>>();
            int startIndex = start - _rangeStart;

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

    public void Add(Dictionary<DefectType, VectorOfVectorOfPoint> contours)
    {
        // Это означает, что буфер заполнен до текущего "ожидаемого" диапазона.
        // Если мы добавим элемент, диапазон нужно будет расширить.
        if (_buffer.Count == (_rangeEnd - _rangeStart))
        {
            // Теперь диапазон ожидает слои от start до нового end (не включая end).
            _rangeEnd++;
        }

        _buffer.AddLast(contours);

        // 3. Управляем размером буфера.
        // Если количество элементов в буфере превышает максимальный размер,
        // удаляем самый старый элемент и корректируем стартовый диапазон.
        while (_buffer.Count > _sizeBuffer)
        {
            var removed = _buffer.First!.Value; // Получаем самый старый элемент (первый в списке)
            _buffer.RemoveFirst();              // Удаляем его из буфера

            // После удаления первого элемента, стартовый слой диапазона сдвигается вперёд.
            _rangeStart++;

            ReleaseMats(removed);
        }
    }

    public void Clear()
    {
        _buffer.Clear();
        _rangeStart = _rangeEnd;
    }

    public bool HasInCache(int curLayer)
    {
        return _rangeStart <= curLayer && curLayer < _rangeEnd;
    }

    /// <summary>
    /// Освобождает ресурсы OpenCV Mat
    /// </summary>
    private void ReleaseMats(Dictionary<DefectType, VectorOfVectorOfPoint> removed)
    {
        foreach (var contours in removed.Values)
        {
            contours?.Dispose();
        }
    }
}
