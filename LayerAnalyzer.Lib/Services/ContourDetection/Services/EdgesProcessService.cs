using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XImgproc;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

namespace LayerAnalyzer.Lib.Services.ContourDetection.Services;

/// <summary>
/// Сервис для обработки границ (edges) на изображениях
/// </summary>
public static class EdgesProcessService
{
    /// <summary>
    /// Обнаружение границ с помощью StructuredEdgeDetection
    /// </summary>
    public static void EdgeDetect(Mat src8UC3, Mat dst8UC1, StructuredEdgeDetection edgeDetector)
    {
        using var srcFloat = new Mat();

        // Конвертируем в float [0, 1]
        src8UC3.ConvertTo(srcFloat, DepthType.Cv32F, 1.0 / 255.0);

        // Обнаруживаем границы
        edgeDetector.DetectEdges(srcFloat, dst8UC1);

        // Масштабируем обратно в [0, 255]
        CvInvoke.Multiply(dst8UC1, new ScalarArray(new MCvScalar(255.0)), dst8UC1);
        CvInvoke.Subtract(dst8UC1, new ScalarArray(new MCvScalar(0.5)), dst8UC1);

        // Конвертируем в 8-bit
        dst8UC1.ConvertTo(dst8UC1, DepthType.Cv8U);
    }

    /// <summary>
    /// Обнаружение границ Sobel (вертикальные + горизонтальные)
    /// </summary>
    public static Mat GetDetectEdgesSobel(Mat src8UC3)
    {
        var dst8UC1 = new Mat(src8UC3.Size, DepthType.Cv8U, 1);
        dst8UC1.SetTo(new MCvScalar(0));

        // Разделяем на каналы
        using var channels = new VectorOfMat();
        CvInvoke.Split(src8UC3, channels);

        // Обрабатываем каждый канал
        for (int i = 0; i < channels.Size; i++)
        {
            using var res = GetChannelOfDetectEdgesSobel(channels[i]);
            CvInvoke.Max(dst8UC1, res, dst8UC1);
        }

        return dst8UC1;
    }

    /// <summary>
    /// Обнаружение вертикальных границ Sobel
    /// </summary>
    public static Mat GetDetectEdgesSobelVertical(Mat src8UC3)
    {
        var dst8UC1 = new Mat(src8UC3.Size, DepthType.Cv8U, 1);
        dst8UC1.SetTo(new MCvScalar(0));

        // Разделяем на каналы
        using var channels = new VectorOfMat();
        CvInvoke.Split(src8UC3, channels);

        // Обрабатываем каждый канал
        for (int i = 0; i < channels.Size; i++)
        {
            using var res = GetChannelOfDetectEdgesSobelVertical(channels[i]);
            CvInvoke.Max(dst8UC1, res, dst8UC1);
        }

        return dst8UC1;
    }

    /// <summary>
    /// Обнаружение горизонтальных границ Sobel
    /// </summary>
    public static Mat GetDetectEdgesSobelHorizontal(Mat src8UC3)
    {
        var dst8UC1 = new Mat(src8UC3.Size, DepthType.Cv8U, 1);
        dst8UC1.SetTo(new MCvScalar(0));

        // Разделяем на каналы
        using var channels = new VectorOfMat();
        CvInvoke.Split(src8UC3, channels);

        // Обрабатываем каждый канал
        for (int i = 0; i < channels.Size; i++)
        {
            using var res = GetChannelOfDetectEdgesSobelHorizontal(channels[i]);
            CvInvoke.Max(dst8UC1, res, dst8UC1);
        }

        return dst8UC1;
    }

    /// <summary>
    /// Sobel для одного канала (вертикальные + горизонтальные)
    /// </summary>
    public static Mat GetChannelOfDetectEdgesSobel(Mat srcChannel)
    {
        using var absGradX = GetChannelOfDetectEdgesSobelVertical(srcChannel);
        using var absGradY = GetChannelOfDetectEdgesSobelHorizontal(srcChannel);

        var dst8UC1 = new Mat();
        CvInvoke.AddWeighted(absGradX, 0.5, absGradY, 0.5, 0, dst8UC1);

        return dst8UC1;
    }

    /// <summary>
    /// Sobel вертикальный для одного канала
    /// </summary>
    public static Mat GetChannelOfDetectEdgesSobelVertical(Mat srcChannel)
    {
        var dstChannel = new Mat();
        CvInvoke.Sobel(srcChannel, dstChannel, DepthType.Cv16S, 1, 0, 3, 1, 0, BorderType.Replicate);
        CvInvoke.ConvertScaleAbs(dstChannel, dstChannel, 1.0, 0);
        return dstChannel;
    }

    /// <summary>
    /// Sobel горизонтальный для одного канала
    /// </summary>
    public static Mat GetChannelOfDetectEdgesSobelHorizontal(Mat srcChannel)
    {
        var dstChannel = new Mat();
        CvInvoke.Sobel(srcChannel, dstChannel, DepthType.Cv16S, 0, 1, 3, 1, 0, BorderType.Replicate);
        CvInvoke.ConvertScaleAbs(dstChannel, dstChannel, 1.0, 0);
        return dstChannel;
    }

    /// <summary>
    /// Шумоподавление границ с помощью итеративного медианного фильтра
    /// </summary>
    public static void DenoiseEdgeBlur(Mat srcChannel, Mat dstChannel, int kSize, int iters)
    {
        var lastMedian = srcChannel.Clone();
        var median = new Mat();

        var medianZero = new Mat();
        var imgZero = new Mat();
        var logicalAnd = new Mat();

        int count = 0;
        srcChannel.CopyTo(dstChannel);

        // Первая медианная фильтрация
        CvInvoke.MedianBlur(srcChannel, median, kSize);
        // Итеративная фильтрация
        while (!(CommonOcvService.IsImgEquals(lastMedian, median) || count > iters))
        {
            lastMedian.Dispose();
            count++;

            // Находим нулевые пиксели
            CvInvoke.Compare(median, new ScalarArray(new MCvScalar(0)), medianZero, CmpType.Equal); //??
            CvInvoke.Compare(dstChannel, new ScalarArray(new MCvScalar(0)), imgZero, CmpType.Equal); // ??
            CvInvoke.BitwiseOr(medianZero, imgZero, logicalAnd);
            // Устанавливаем их в ноль
            dstChannel.SetTo(new MCvScalar(0), logicalAnd);

            lastMedian = median.Clone();
            CvInvoke.MedianBlur(dstChannel, median, kSize);
        }

        lastMedian.Dispose();
        median.Dispose();
        medianZero.Dispose();
        imgZero.Dispose();
        logicalAnd.Dispose();
    }
}
