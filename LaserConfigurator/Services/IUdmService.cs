using Hans.NET.Models;
using LaserConfigurator.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Сервис генерации UDM файлов для Hans сканаторов
    /// </summary>
    public interface IUdmService
    {
        /// <summary>
        /// Сгенерировать UDM данные из геометрии
        /// </summary>
        Task<string> GenerateUdmDataAsync(
            ScanatorConfiguration config,
            List<(float x, float y)> points,
            ShapeParameters parameters);

        /// <summary>
        /// Сгенерировать два UDM файла для двух сканаторов
        /// </summary>
        Task<(string scanner1Data, string scanner2Data)> GenerateDualUdmDataAsync(
            ScanatorConfiguration scanner1Config,
            ScanatorConfiguration scanner2Config,
            List<(float x, float y)> part1Points,
            List<(float x, float y)> part2Points,
            ShapeParameters parameters);
    }
}
