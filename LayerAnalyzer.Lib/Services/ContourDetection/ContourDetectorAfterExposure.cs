using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.ContourDetection;
using LayerAnalyzer.Lib.Services.ContourDetection.Services;
using System.Drawing;
using Emgu.CV.Structure;

namespace LayerAnalyzer.Lib.Services.ContourDetection;

/// <summary>
/// Детектор контуров после лазерного воздействия
/// Использует полный конвейер обработки: PMAD → StructuredEdgeDetection → GrabCut → Sort → Approx
/// </summary>
public class ContourDetectorAfterExposure : IContourDetector
{
    public VectorOfVectorOfPoint GetContours(Mat image)
    {
        using var denoised = new Mat(image.Size, image.Depth, image.NumberOfChannels);
        using var edgeDetectChannel = new Mat(image.Size, image.Depth, image.NumberOfChannels);

        // 1. Шумоподавление с помощью PMAD (Perona-Malik Anisotropic Diffusion)
        ImageProcessService.DenoisePMAD(image, denoised, 0.09f, 0.05f, 20);

        // 2. Детектирование границ с помощью StructuredEdgeDetection
        var edgeDetector = EdgeDetector.Instance.GetStructEdgeDetector();
        if (edgeDetector != null)
        {
            EdgesProcessService.EdgeDetect(denoised, edgeDetectChannel, edgeDetector);
        }
        else
        {
            // Fallback на Canny, если модель не загружена
            using var gray = new Mat();
            CvInvoke.CvtColor(denoised, gray, ColorConversion.Bgr2Gray);
            CvInvoke.Canny(gray, edgeDetectChannel, 50, 150);

            //
            var kernel = CvInvoke.GetStructuringElement(
                MorphShapes.Rectangle,
                new Size(3, 3),
                new Point(-1, -1));
            using var thickEdges = new Mat();
            CvInvoke.Dilate(edgeDetectChannel, thickEdges, kernel, new Point(-1,-1),1,BorderType.Constant,new MCvScalar(0));
            thickEdges.CopyTo(edgeDetectChannel);
            kernel.Dispose();
            //
        }

        // 3. Шумоподавление границ
        EdgesProcessService.DenoiseEdgeBlur(edgeDetectChannel, edgeDetectChannel, 3, 50);

        // 4. Поиск контуров
        var contours = FindContoursProcessService.GetFoundContours(edgeDetectChannel);

        // 5. Улучшение контуров с помощью GrabCut (параллельная версия)
        FindContoursProcessService.EnhanceSelectionContoursPolyParallel(image, contours, new Size(3, 3), 3);

        // 6. Сортировка контуров по площади
        ContoursProcessService.SortContoursByArea(contours);

        // 7. Аппроксимация контуров полигонами
        var contoursPoly = ContoursProcessService.GetApproxContoursPoly(contours, 0.9);

        return contoursPoly;
    }
}
