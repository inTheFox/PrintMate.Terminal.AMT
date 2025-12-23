using LaserConfigurator.Models;
using System.Collections.Generic;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Сервис генерации геометрических фигур
    /// </summary>
    public interface IGeometryService
    {
        /// <summary>
        /// Сгенерировать точки для фигуры
        /// </summary>
        List<(float x, float y)> GenerateShape(ShapeParameters parameters);

        /// <summary>
        /// Разделить фигуру на две части для двух лазеров
        /// </summary>
        (List<(float x, float y)> part1, List<(float x, float y)> part2) SplitShape(List<(float x, float y)> points);
    }
}
