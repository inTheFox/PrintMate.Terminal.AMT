using LaserConfigurator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Реализация сервиса генерации геометрических фигур
    /// </summary>
    public class GeometryService : IGeometryService
    {
        public List<(float x, float y)> GenerateShape(ShapeParameters parameters)
        {
            return parameters.Type switch
            {
                ShapeType.Square => GenerateSquare(parameters),
                ShapeType.Circle => GenerateCircle(parameters),
                ShapeType.Triangle => GenerateTriangle(parameters),
                ShapeType.Cross => GenerateCross(parameters),
                _ => new List<(float x, float y)>()
            };
        }

        private List<(float x, float y)> GenerateSquare(ShapeParameters parameters)
        {
            float halfSize = (float)parameters.Size / 2;
            float centerX = (float)parameters.X;
            float centerY = (float)parameters.Y;

            var points = new List<(float x, float y)>
            {
                (centerX - halfSize, centerY - halfSize), // Левый нижний
                (centerX + halfSize, centerY - halfSize), // Правый нижний
                (centerX + halfSize, centerY + halfSize), // Правый верхний
                (centerX - halfSize, centerY + halfSize), // Левый верхний
                (centerX - halfSize, centerY - halfSize)  // Замыкание
            };

            return points;
        }

        private List<(float x, float y)> GenerateCircle(ShapeParameters parameters)
        {
            float radius = (float)parameters.Size / 2;
            float centerX = (float)parameters.X;
            float centerY = (float)parameters.Y;

            int segments = 360; // Количество сегментов для круга
            var points = new List<(float x, float y)>();

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = centerY + radius * (float)Math.Sin(angle);
                points.Add((x, y));
            }

            return points;
        }

        private List<(float x, float y)> GenerateTriangle(ShapeParameters parameters)
        {
            float size = (float)parameters.Size;
            float centerX = (float)parameters.X;
            float centerY = (float)parameters.Y;

            // Равносторонний треугольник
            float height = size * (float)Math.Sqrt(3) / 2;

            var points = new List<(float x, float y)>
            {
                (centerX, centerY + height * 2 / 3),           // Верхняя вершина
                (centerX - size / 2, centerY - height / 3),    // Левая нижняя
                (centerX + size / 2, centerY - height / 3),    // Правая нижняя
                (centerX, centerY + height * 2 / 3)            // Замыкание
            };

            return points;
        }

        private List<(float x, float y)> GenerateCross(ShapeParameters parameters)
        {
            float halfSize = (float)parameters.Size / 2;
            float centerX = (float)parameters.X;
            float centerY = (float)parameters.Y;

            // Крест состоит из двух линий:
            // Горизонтальная линия: от (center - halfSize, center) до (center + halfSize, center)
            // Вертикальная линия: от (center, center - halfSize) до (center, center + halfSize)
            // Рисуем как один непрерывный путь: горизонталь -> возврат в центр -> вертикаль
            var points = new List<(float x, float y)>
            {
                // Горизонтальная линия
                (centerX - halfSize, centerY),  // Левый конец
                (centerX + halfSize, centerY),  // Правый конец
                // Возврат в центр
                (centerX, centerY),             // Центр
                // Вертикальная линия
                (centerX, centerY - halfSize),  // Нижний конец
                (centerX, centerY + halfSize)   // Верхний конец
            };

            return points;
        }

        public (List<(float x, float y)> part1, List<(float x, float y)> part2) SplitShape(List<(float x, float y)> points)
        {
            if (points.Count == 0)
            {
                return (new List<(float x, float y)>(), new List<(float x, float y)>());
            }

            // Находим центр фигуры по Y (горизонтальное разделение)
            float centerY = points.Average(p => p.y);

            // Собираем только точки, принадлежащие каждой половине
            var bottomPoints = new List<(float x, float y)>();  // part1 - нижняя часть
            var topPoints = new List<(float x, float y)>();     // part2 - верхняя часть

            for (int i = 0; i < points.Count - 1; i++)
            {
                var currentPoint = points[i];
                var nextPoint = points[i + 1];

                bool currentIsBottom = currentPoint.y <= centerY;
                bool nextIsBottom = nextPoint.y <= centerY;

                if (currentIsBottom && nextIsBottom)
                {
                    // Оба точки снизу - добавляем текущую
                    bottomPoints.Add(currentPoint);
                }
                else if (!currentIsBottom && !nextIsBottom)
                {
                    // Оба точки сверху - добавляем текущую
                    topPoints.Add(currentPoint);
                }
                else
                {
                    // Линия пересекает центр - нужно найти точку пересечения
                    float deltaY = nextPoint.y - currentPoint.y;

                    // Защита от деления на ноль (горизонтальная линия)
                    if (Math.Abs(deltaY) < 0.0001f)
                    {
                        continue;
                    }

                    float t = (centerY - currentPoint.y) / deltaY;
                    float intersectX = currentPoint.x + t * (nextPoint.x - currentPoint.x);
                    var intersectionPoint = (intersectX, centerY);

                    if (currentIsBottom)
                    {
                        // Идём снизу вверх - завершаем нижний сегмент
                        bottomPoints.Add(currentPoint);
                        bottomPoints.Add(intersectionPoint);
                    }
                    else
                    {
                        // Идём сверху вниз - завершаем верхний сегмент
                        topPoints.Add(currentPoint);
                        topPoints.Add(intersectionPoint);
                    }
                }
            }

            // Добавляем последнюю точку если она в своей половине
            if (points.Count > 1)
            {
                var lastPoint = points[^1];
                if (lastPoint.y <= centerY && bottomPoints.Count > 0)
                {
                    // Не добавляем если это замыкание (совпадает с первой)
                    if (bottomPoints[0] != lastPoint)
                        bottomPoints.Add(lastPoint);
                }
                else if (lastPoint.y > centerY && topPoints.Count > 0)
                {
                    if (topPoints[0] != lastPoint)
                        topPoints.Add(lastPoint);
                }
            }

            return (bottomPoints, topPoints);
        }
    }
}
