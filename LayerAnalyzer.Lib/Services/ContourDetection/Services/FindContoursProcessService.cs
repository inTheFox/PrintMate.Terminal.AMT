using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.ContourDetection;

namespace LayerAnalyzer.Lib.Services.ContourDetection.Services;

/// <summary>
/// Сервис для поиска и улучшения контуров с помощью GrabCut
/// </summary>
public static class FindContoursProcessService
{
    /// <summary>
    /// Найти контуры на изображении границ
    /// </summary>
    public static VectorOfVectorOfPoint GetFoundContours(Mat edges)
    {
        using var hierarchy = new Mat();
        var contours = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(edges, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxTc89Kcos);
        return contours;
    }

    /// <summary>
    /// Улучшить выделение контуров с помощью GrabCut (параллельная версия)
    /// </summary>
    public static void EnhanceSelectionContoursPolyParallel(Mat img, VectorOfVectorOfPoint contoursPoly,
        Size kernelSize, int nIter)
    {
        // Создаём список crop info
        var cropped = CropInfo.FromContours(img, contoursPoly);

        // Очищаем старые контуры
        contoursPoly.Clear();

        // Обрабатываем каждую вырезанную область
        var enhancedContours = new List<VectorOfVectorOfPoint>();

        // В C# используем Parallel.ForEach для параллельной обработки
        object lockObj = new object();
        Parallel.ForEach(cropped, cropInfo =>
        {
            var enhanced = GetEnhancedCroppedContour(cropInfo, kernelSize, nIter);
            lock (lockObj)
            {
                enhancedContours.Add(enhanced);
            }
        });

        // Объединяем результаты
        foreach (var enhanced in enhancedContours)
        {
            for (int i = 0; i < enhanced.Size; i++)
            {
                contoursPoly.Push(enhanced[i]);
            }
            enhanced.Dispose();
        }

        // Освобождаем ресурсы
        foreach (var crop in cropped)
        {
            crop.Release();
        }
    }

    /// <summary>
    /// Улучшить выделение контуров с помощью GrabCut (последовательная версия)
    /// </summary>
    public static void EnhanceSelectionContoursPoly(Mat img, VectorOfVectorOfPoint contoursPoly,
        Size kernelSize, int nIter)
    {
        var cropped = CropInfo.FromContours(img, contoursPoly);
        contoursPoly.Clear();

        foreach (var cropInfo in cropped)
        {
            var enhanced = GetEnhancedCroppedContour(cropInfo, kernelSize, nIter);
            for (int i = 0; i < enhanced.Size; i++)
            {
                contoursPoly.Push(enhanced[i]);
            }
            enhanced.Dispose();
        }

        foreach (var crop in cropped)
        {
            crop.Release();
        }
    }

    /// <summary>
    /// Получить улучшенные контуры для вырезанной области с помощью GrabCut
    /// </summary>
    public static VectorOfVectorOfPoint GetEnhancedCroppedContour(CropInfo cropInfo, Size kernelSize, int nIter)
    {
        var croppedContours = new VectorOfVectorOfPoint();

        using var kernelMatrix = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, kernelSize, new Point(-1, -1));
        using var bgdModel = new Mat();
        using var fgdModel = new Mat();
        using var croppedMask = cropInfo.GetContourMask();
        using var croppedImg = cropInfo.GetImg();
        var croppedRect = cropInfo.GetRect();

        using var trimap = new Mat(croppedRect.Size, DepthType.Cv8U, 1);
        trimap.SetTo(new MCvScalar(0));

        using var foundMatrix = new Mat(croppedRect.Size, DepthType.Cv8U, 1);
        using var mapForeground = new Mat(croppedRect.Size, DepthType.Cv8U, 1);
        using var mask = new Mat(croppedRect.Size, DepthType.Cv8U, 1);
        mask.SetTo(new MCvScalar(0));

        // Создаём trimap: 0 = фон, 1 = foreground, 2 = возможно foreground, 3 = возможно фон
        CvInvoke.Compare(croppedMask, new ScalarArray(new MCvScalar(255)), foundMatrix, CmpType.Equal);
        trimap.SetTo(new MCvScalar((int)TrimapClasses.PrFgd), foundMatrix); // 2 = вероятно foreground

        // Эрозия для получения определённого foreground
        CvInvoke.Erode(croppedMask, mapForeground, kernelMatrix, new Point(-1, -1), nIter, BorderType.Constant, new MCvScalar(0));

        CvInvoke.Compare(mapForeground, new ScalarArray(new MCvScalar(255)), foundMatrix, CmpType.Equal);
        trimap.SetTo(new MCvScalar((int)TrimapClasses.FGD), foundMatrix); // 1 = точно foreground

        // Применяем GrabCut
        int nonZeroCount = CvInvoke.CountNonZero(foundMatrix);
        int totalPixels = croppedRect.Width * croppedRect.Height;

        // Проверяем, что есть и фон, и передний план
        // nonZeroCount = количество пикселей переднего плана (FGD)
        // totalPixels - nonZeroCount = количество пикселей фона/вероятного фона
        bool hasForeground = nonZeroCount > 0;
        bool hasBackground = (totalPixels - nonZeroCount) > 0;

        if (hasForeground && hasBackground)
        {
            try
            {
                CvInvoke.GrabCut(croppedImg, trimap, croppedRect, bgdModel, fgdModel, 1, GrabcutInitType.InitWithMask);
            }
            catch (CvException)
            {
                // Если GrabCut всё равно упал, используем исходную маску
                trimap.CopyTo(mask);
            }
        }
        else if (croppedRect.Width > 5 && croppedRect.Height > 5)
        {
            try
            {
                var initRect = new Rectangle(0, 0, croppedRect.Width - 1, croppedRect.Height - 1);
                CvInvoke.GrabCut(croppedImg, trimap, initRect, bgdModel, fgdModel, 1, GrabcutInitType.InitWithRect);
            }
            catch (CvException)
            {
                // Если GrabCut упал, используем простую маску
                trimap.SetTo(new MCvScalar(0));
            }
        }
        else
        {
            // Слишком маленькая область - пропускаем GrabCut
            trimap.SetTo(new MCvScalar(0));
        }

        // Создаём финальную маску из foreground (1) и возможно foreground (3)
        CvInvoke.Compare(trimap, new ScalarArray(new MCvScalar((int)TrimapClasses.PrBgd)), foundMatrix, CmpType.Equal);
        mask.SetTo(new MCvScalar(255), foundMatrix);

        CvInvoke.Compare(trimap, new ScalarArray(new MCvScalar((int)TrimapClasses.FGD)), foundMatrix, CmpType.Equal);
        mask.SetTo(new MCvScalar(255), foundMatrix);

        // Находим контуры на маске
        if (CvInvoke.CountNonZero(mask) > 0)
        {
            using var hierarchy = new Mat();
            CvInvoke.FindContours(mask, croppedContours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxTc89Kcos);

            // Смещаем контуры обратно к исходным координатам
            // Создаём новый VectorOfVectorOfPoint для смещённых контуров
            var offsetContours = new VectorOfVectorOfPoint();
            for (int i = 0; i < croppedContours.Size; i++)
            {
                var points = croppedContours[i].ToArray();
                for (int j = 0; j < points.Length; j++)
                {
                    points[j].X += croppedRect.X;
                    points[j].Y += croppedRect.Y;
                }
                offsetContours.Push(new VectorOfPoint(points));
            }

            // Заменяем croppedContours на offsetContours
            croppedContours.Dispose();
            croppedContours = offsetContours;
        }

        return croppedContours;
    }

    /// <summary>
    /// Классы для GrabCut trimap
    /// </summary>
    private enum TrimapClasses
    {
        BGD = 0,      // Определённо фон
        FGD = 1,      // Определённо передний план
        PrBgd = 2,    // Вероятно фон
        PrFgd = 3     // Вероятно передний план
    }
}
