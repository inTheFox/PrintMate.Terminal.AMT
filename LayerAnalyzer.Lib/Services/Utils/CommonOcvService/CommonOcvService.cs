using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Text.RegularExpressions;

namespace LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

/// <summary>
/// Общие утилиты для работы с OpenCV
/// </summary>
public static class CommonOcvService
{
    /// <summary>
    /// Проверяет, равны ли два изображения
    /// </summary>
    public static bool IsImgEquals(Mat img1, Mat img2)
    {
        using Mat resultMatrix = new();
        CvInvoke.Compare(img1, img2, resultMatrix, CmpType.Equal);
        bool res = CvInvoke.CountNonZero(resultMatrix) == img1.Width * img1.Height;

        return res;
    }
}
