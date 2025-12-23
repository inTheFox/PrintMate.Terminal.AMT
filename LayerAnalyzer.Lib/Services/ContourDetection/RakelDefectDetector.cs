using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Services.ContourDetection.Services;

namespace LayerAnalyzer.Lib.Services.ContourDetection;

/// <summary>
/// Детектор дефектов от ракеля (вертикальные линии от лезвия для разравнивания порошка)
/// </summary>
public class RakelDefectDetector : IContourDetector
{
    public VectorOfVectorOfPoint GetContours(Mat image)
    {
        using var denoised = new Mat(image.Size, DepthType.Cv8U, 3);
        using var verticalChannel = new Mat(image.Size, DepthType.Cv8U, 1);
        using var kernel = new Mat(new Size(7, 1), DepthType.Cv8U, 1);
        kernel.SetTo(new MCvScalar(1));

        // Применяем Gaussian blur для шумоподавления
        ImageProcessService.DenoiseGaussianBlur(image, denoised, new Size(5, 5), 0.0f, 0.0f);

        // Получаем вертикальные края Sobel
        using var edgeVerticalChannel = EdgesProcessService.GetDetectEdgesSobelVertical(denoised);
        // Все пиксели > 10 становятся 0, остальные остаются как есть
        var a = edgeVerticalChannel.GetRawData().Where(x => x < 10);
        CvInvoke.Threshold(edgeVerticalChannel, verticalChannel, 10, 255,
            ThresholdType.BinaryInv | ThresholdType.Otsu);

        // Инвертируем
        CvInvoke.BitwiseNot(verticalChannel, verticalChannel);

        // Эрозия с вертикальным структурирующим элементом
        int verticalSize = verticalChannel.Rows / 30;
        using var verticalStructure1 = CvInvoke.GetStructuringElement(MorphShapes.Rectangle,
            new Size(1, verticalSize), new Point(-1, -1));
        CvInvoke.Erode(verticalChannel, verticalChannel, verticalStructure1, new Point(-1, -1),
            1, BorderType.Constant, new MCvScalar(0));
        // Дилатация с вертикальным структурирующим элементом (полная высота)
        using var verticalStructure2 = CvInvoke.GetStructuringElement(MorphShapes.Rectangle,
            new Size(1, verticalChannel.Rows), new Point(-1, -1));
        CvInvoke.Dilate(verticalChannel, verticalChannel, verticalStructure2, new Point(-1, -1),
            1, BorderType.Constant, new MCvScalar(0));
        // Дилатация с горизонтальным ядром
        CvInvoke.Dilate(verticalChannel, verticalChannel, kernel, new Point(-1, -1),
            1, BorderType.Constant, new MCvScalar(0));

        // Сканируем столбцы для поиска вертикальных линий
        int rowSize = verticalChannel.Rows;
        int startLineColIndex = -1;
        var lines = new List<(int start, int end)>();

        for (int colIndex = 0; colIndex < verticalChannel.Cols; colIndex++)
        {
            using var col = new Mat(verticalChannel, new Rectangle(colIndex, 0, 1, rowSize));
            int whitePixelsInCol = CvInvoke.CountNonZero(col);

            if (whitePixelsInCol > rowSize * 0.95)
            {
                // Начало линии
                if (startLineColIndex == -1)
                {
                    startLineColIndex = colIndex;
                }
            }
            else if (startLineColIndex != -1)
            {
                // Конец линии
                lines.Add((startLineColIndex, colIndex - 1));
                startLineColIndex = -1;
            }
        }

        // Если линия доходит до конца изображения
        if (startLineColIndex != -1)
        {
            lines.Add((startLineColIndex, verticalChannel.Cols - 1));
        }

        // Создаём контуры из найденных линий
        var contours = new VectorOfVectorOfPoint();
        foreach (var (start, end) in lines)
        {
            var points = new[]
            {
                new Point(start, 0),
                new Point(end, 0),
                new Point(end, rowSize - 1),
                new Point(start, rowSize - 1)
            };
            contours.Push(new VectorOfPoint(points));
        }

        return contours;
    }
}
