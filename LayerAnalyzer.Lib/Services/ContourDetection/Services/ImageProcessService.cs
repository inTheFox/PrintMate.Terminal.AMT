using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.XImgproc;

namespace LayerAnalyzer.Lib.Services.ContourDetection.Services;

/// <summary>
/// Сервис для обработки изображений (denoising, enhancement)
/// </summary>
public static class ImageProcessService
{
    /// <summary>
    /// Применить PMAD (Perona-Malik Anisotropic Diffusion) шумоподавление
    /// </summary>
    public static void DenoisePMAD(Mat src, Mat dst, float alpha, float K, int niters)
    {
        // Сначала улучшаем контраст
        Enhance(src, dst);

        XImgprocInvoke.AnisotropicDiffusion(dst,dst,alpha,K,niters);
    }

    /// <summary>
    /// Применить Gaussian Blur шумоподавление
    /// </summary>
    public static void DenoiseGaussianBlur(Mat src, Mat dst, Size kernel, float sigmaX, float sigmaY)
    {
        // Сначала улучшаем контраст
        Enhance(src, dst);

        // Применяем Gaussian blur
        CvInvoke.GaussianBlur(dst, dst, kernel, sigmaX, sigmaY);
    }

    /// <summary>
    /// Улучшить изображение с помощью CLAHE (Contrast Limited Adaptive Histogram Equalization)
    /// </summary>
    public static void Enhance(Mat src, Mat dst)
    {
        using var lab = new Mat();
        using var cl = new Mat();

        // Преобразуем BGR в LAB
        CvInvoke.CvtColor(src, lab, ColorConversion.Bgr2Lab);

        // Разделяем на каналы
        using var channelsLab = new VectorOfMat();
        CvInvoke.Split(lab, channelsLab);

        // Применяем CLAHE к L-каналу (яркость)
        CvInvoke.CLAHE(channelsLab[0], 2.0, new Size(8, 8), cl);

        // Заменяем L-канал (копируем cl в первый канал вместо присваивания)
        cl.CopyTo(channelsLab[0]);

        // Объединяем каналы обратно
        CvInvoke.Merge(channelsLab, dst);

        // Преобразуем LAB обратно в BGR
        CvInvoke.CvtColor(dst, dst, ColorConversion.Lab2Bgr);
    }
}
