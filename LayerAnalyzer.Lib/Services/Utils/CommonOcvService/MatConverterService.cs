using Emgu.CV.Util;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;
using Emgu.CV;

namespace LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

/// <summary>
/// Утилиты для конвертации Mat в различные форматы
/// </summary>
public static class MatConverterService
{
    public static Mat BitmapSourceToMat(BitmapSource bitmapSource)
    {
        return bitmapSource.ToMat();
    }

    /// <summary>
    /// Конвертирует список VectorOfPointF в список VectorOfPoint
    /// </summary>
    public static VectorOfVectorOfPoint VectorOfPointFToVectorOfPoint(VectorOfVectorOfPointF vectorOfPointF)
    {
        VectorOfVectorOfPoint result = new();

        for (int i = 0; i < vectorOfPointF.Size; i++)
        {
            using VectorOfPointF contour = vectorOfPointF[i];
            PointF[] pointsF = contour.ToArray();
            Point[] points = new Point[pointsF.Length];

            for (int j = 0; j < pointsF.Length; j++)
            {
                points[j] = new Point((int)pointsF[j].X, (int)pointsF[j].Y);
            }

            result.Push(new VectorOfPoint(points));
        }

        return result;
    }

    /// <summary>
    /// Конвертирует VectorOfPoint в VectorOfPointF
    /// </summary>
    public static VectorOfPointF VectorOfPointToVectorOfPointF(VectorOfPoint vectorOfPoint)
    {
        Point[] points = vectorOfPoint.ToArray();
        PointF[] pointsF = new PointF[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            pointsF[i] = new PointF(points[i].X, points[i].Y);
        }

        return new VectorOfPointF(pointsF);
    }

    /// <summary>
    /// Конвертирует список VectorOfPoint в список VectorOfPointF
    /// </summary>
    public static VectorOfVectorOfPointF VectorOfPointToVectorOfPointF(VectorOfVectorOfPoint vectorOfPoint)
    {
        VectorOfVectorOfPointF result = new();

        for (int i = 0; i < vectorOfPoint.Size; i++)
        {
            using VectorOfPoint contour = vectorOfPoint[i];
            result.Push(VectorOfPointToVectorOfPointF(contour));
        }

        return result;
    }

    /// <summary>
    /// Создаёт VectorOfPointF из Range (прямоугольник по высоте кадра)
    /// </summary>
    public static VectorOfPointF GetVectorOfPointFromRange(int rangeStart, int rangeEnd, int frameHeight)
    {
        PointF[] pointsContour = new PointF[]
        {
            new PointF(rangeStart, 0),
            new PointF(rangeEnd, 0),
            new PointF(rangeEnd, frameHeight),
            new PointF(rangeStart, frameHeight)
        };

        return new VectorOfPointF(pointsContour);
    }

    /// <summary>
    /// Конвертирует GraphicsPath в VectorOfPointF
    /// </summary>
    public static VectorOfPointF ConvertPath2Mat(GraphicsPath path)
    {
        List<PointF> points = new();

        // Получаем все точки из GraphicsPath
        PointF[] pathPoints = path.PathPoints;
        byte[] pathTypes = path.PathTypes;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            // Добавляем точки MoveTo и LineTo
            byte type = (byte)(pathTypes[i] & 0x07); // Маска для получения типа
            if (type == 0 || type == 1) // MoveTo или LineTo
            {
                points.Add(pathPoints[i]);
            }
        }


        return new VectorOfPointF(points.ToArray());
    }
    /// <summary>
    /// Конвертирует VectorOfPointF в GraphicsPath
    /// </summary>
    public static GraphicsPath ConvertMat2Path(VectorOfPointF mat)
    {
        return ConvertMat2Path(mat, 1);
    }

    /// <summary>
    /// Конвертирует VectorOfPointF в GraphicsPath с масштабированием
    /// </summary>
    public static GraphicsPath ConvertMat2Path(VectorOfPointF mat, int ratio)
    {
        GraphicsPath path = new();
        PointF[] matPointsMm = mat.ToArray();

        if (matPointsMm.Length > 0)
        {
            // Начинаем путь с первой точки
            path.StartFigure();

            for (int i = 0; i < matPointsMm.Length; i++)
            {
                float x = matPointsMm[i].X * ratio;
                float y = matPointsMm[i].Y * ratio;

                if (i == 0)
                {
                    path.AddLine(x, y, x, y); // Первая точка
                }
                else
                {
                    path.AddLine(matPointsMm[i - 1].X * ratio, matPointsMm[i - 1].Y * ratio, x, y);
                }
            }

            path.CloseFigure();
        }

        return path;
    }
}
