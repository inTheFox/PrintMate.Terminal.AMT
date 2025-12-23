using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PrintMate.Terminal.Parsers.Shared;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Interfaces;
using ProjectParserTest.Parsers.Shared.Models;
using RegionModel = ProjectParserTest.Parsers.Shared.Models.Region;

namespace PrintMate.Terminal.Parsers.CncParser
{
    /// <summary>
    /// Парсер CNC (G-code) файлов для лазерной печати
    /// </summary>
    public class CncProvider : IParserProvider
    {
        private static readonly Regex _gCodeRegex = new Regex(@"^([GM]\d+)", RegexOptions.Compiled);
        private static readonly Regex _parameterRegex = new Regex(@"([XYZPSF])(-?\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex _commentConfigRegex = new Regex(@";\s*(\w+)\s*:\s*(.+)", RegexOptions.Compiled);

        public Project Project { get; set; } = null;

        public event Action<string> ParseStarted;
        public event Action<Project> ParseCompleted;
        public event Action<string> ParseError;
        public event Action<double> ParseProgressChanged;

        private double _totalLinesCount = 0;
        private int _currentLineNumber = 0;

        public CncProvider()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public Task NextLayer()
        {
            var nextLayer = Project.Layers.ElementAtOrDefault(Project.CurrentLayer.Id + 1);
            if (nextLayer != null)
            {
                Project.CurrentLayer = nextLayer;
                Console.WriteLine($"\nNEXT LAYER ID {nextLayer.Id}\n");
            }
            return Task.CompletedTask;
        }

        public async Task<Project> ParseAsync(string path)
        {
            try
            {
                ParseStarted?.Invoke(path);

                // Определяем, это файл или директория
                bool isDirectory = Directory.Exists(path);
                bool isFile = File.Exists(path);

                if (!isDirectory && !isFile)
                {
                    throw new FileNotFoundException($"Path not found: {path}");
                }

                // Создаем проект
                Project = new Project
                {
                    ProjectInfo = new ProjectInfo
                    {
                        Name = isFile ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path),
                        Path = path,
                    },
                    Layers = new List<Layer>(),
                    HeaderInfo = new Data(),
                    Configuration = new Data(),
                };

                // Инициализируем дефолтную деталь
                var defaultPart = new Part { Id = 0, Name = "Default Part" };
                Project.HeaderInfo.AddParameter(HeaderKeys.Info.Parts).SetValue(new List<Part> { defaultPart });

                List<string> cncFiles;

                if (isFile)
                {
                    // Один CNC файл
                    cncFiles = new List<string> { path };
                }
                else
                {
                    // Директория с CNC файлами
                    cncFiles = Directory.GetFiles(path, "*.cnc", SearchOption.TopDirectoryOnly)
                                       .OrderBy(f => f)
                                       .ToList();

                    if (cncFiles.Count == 0)
                    {
                        throw new Exception($"No .cnc files found in directory: {path}");
                    }
                }

                Console.WriteLine($"Found {cncFiles.Count} CNC file(s) to parse");

                // Парсим все файлы
                foreach (var cncFile in cncFiles)
                {
                    await ParseCncFile(cncFile);
                }

                // Удаляем пустые слои
                Project.Layers.RemoveAll(layer => layer.Regions == null || layer.Regions.Count == 0);

                // Пересортируем слои по Id
                Project.Layers = Project.Layers.OrderBy(l => l.Id).ToList();

                // Пересчитываем метаданные проекта
                Project.ProjectInfo.ProjectHeight = Project.GetProjectHeight();
                Project.ProjectInfo.LayerSliceHeight = Project.GetLayerThicknessInMillimeters();
                Project.ProjectInfo.ProjectLink = Project;
                Project.ProjectInfo.MaterialName = Project.GetMaterialName();
                Project.ProjectInfo.PrintTime = Project.GetPrintTimeFormatted();

                if (Project.Layers.Count > 0)
                {
                    Project.CurrentLayer = Project.Layers[0];
                }

                ParseCompleted?.Invoke(Project);
                return Project;
            }
            catch (Exception e)
            {
                Console.WriteLine($"CNC Parse Error: {e}");
                ParseError?.Invoke(e.Message);
                throw;
            }
        }

        private async Task ParseCncFile(string filePath)
        {
            Console.WriteLine($"Parsing CNC file: {filePath}");

            string[] lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            _totalLinesCount = lines.Length;
            _currentLineNumber = 0;

            // Состояние парсера
            Layer currentLayer = null;
            RegionModel currentRegion = null;
            PolyLine currentPolyLine = null;
            float currentX = 0f, currentY = 0f, currentZ = 0f;
            bool laserOn = false;
            double currentPower = 100.0;
            double currentSpeed = 1000.0;
            GeometryRegion currentRegionType = GeometryRegion.None;
            double currentBeamDiameter = 100.0;
            double currentHatchDistance = 0.0;
            double currentHatchAngle = 0.0;

            foreach (var rawLine in lines)
            {
                _currentLineNumber++;

                // Обновляем прогресс каждые 100 строк
                if (_currentLineNumber % 100 == 0)
                {
                    double progress = (_currentLineNumber / _totalLinesCount) * 100.0;
                    ParseProgressChanged?.Invoke(progress);
                }

                string line = rawLine.Trim();

                // Пропускаем пустые строки
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Обработка комментариев (могут содержать конфигурацию)
                if (line.StartsWith(CncSyntax.Comments.Semicolon))
                {
                    ParseComment(line, ref currentRegionType, ref currentBeamDiameter,
                                ref currentHatchDistance, ref currentHatchAngle);
                    continue;
                }

                // Удаляем inline комментарии
                int commentIndex = line.IndexOf(';');
                if (commentIndex >= 0)
                {
                    line = line.Substring(0, commentIndex).Trim();
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Парсим G/M коды
                var match = _gCodeRegex.Match(line);
                if (!match.Success)
                    continue;

                string command = match.Groups[1].Value.ToUpper();
                var parameters = ParseParameters(line);

                switch (command)
                {
                    case "G0": // Быстрое перемещение (лазер выключен)
                        if (laserOn && currentPolyLine != null && currentPolyLine.Points?.Count > 0)
                        {
                            // Завершаем текущую полилинию
                            if (currentRegion != null)
                            {
                                currentRegion.PolyLines.Add(currentPolyLine);
                            }
                        }
                        currentPolyLine = null;

                        UpdatePosition(parameters, ref currentX, ref currentY, ref currentZ);

                        // Если изменилась Z координата - новый слой
                        if (parameters.ContainsKey('Z'))
                        {
                            if (currentRegion != null && currentRegion.PolyLines.Count > 0)
                            {
                                currentLayer?.Regions.Add(currentRegion);
                                currentRegion = null;
                            }

                            currentLayer = GetOrCreateLayer((int)Math.Round(currentZ * 1000)); // Z в мм, Id в микронах
                        }
                        break;

                    case "G1": // Линейное перемещение (лазер может быть включен)
                        float prevX = currentX;
                        float prevY = currentY;
                        UpdatePosition(parameters, ref currentX, ref currentY, ref currentZ);

                        if (laserOn)
                        {
                            // Лазер включен - рисуем
                            if (currentLayer == null)
                            {
                                currentLayer = GetOrCreateLayer(0);
                            }

                            if (currentRegion == null)
                            {
                                currentRegion = CreateRegion(currentRegionType, currentPower, currentSpeed,
                                                            currentBeamDiameter, currentHatchDistance, currentHatchAngle);
                            }

                            if (currentPolyLine == null)
                            {
                                currentPolyLine = new PolyLine();
                                currentPolyLine.Add(new ProjectParserTest.Parsers.Shared.Models.Point(prevX, prevY));
                            }

                            currentPolyLine.Add(new ProjectParserTest.Parsers.Shared.Models.Point(currentX, currentY));

                            // Обновляем длину экспозиции
                            double distance = Math.Sqrt(Math.Pow(currentX - prevX, 2) + Math.Pow(currentY - prevY, 2));
                            currentRegion.ExposeLength += distance;
                        }
                        break;

                    case "M3": // Включить лазер
                        laserOn = true;
                        currentPolyLine = new PolyLine();
                        break;

                    case "M5": // Выключить лазер
                        if (laserOn && currentPolyLine != null && currentPolyLine.Points?.Count > 0)
                        {
                            currentRegion?.PolyLines.Add(currentPolyLine);
                        }

                        laserOn = false;
                        currentPolyLine = null;

                        // Завершаем текущий регион
                        if (currentRegion != null && currentRegion.PolyLines.Count > 0)
                        {
                            currentLayer?.Regions.Add(currentRegion);
                            currentRegion = null;
                        }
                        break;

                    case "M702": // Установить мощность лазера
                        if (parameters.ContainsKey('P'))
                        {
                            currentPower = parameters['P'];
                        }
                        break;

                    case "M704": // Установить скорость лазера
                        if (parameters.ContainsKey('S'))
                        {
                            currentSpeed = parameters['S'];
                        }
                        break;
                }
            }

            // Добавляем последний регион и слой
            if (currentRegion != null && currentRegion.PolyLines.Count > 0)
            {
                currentLayer?.Regions.Add(currentRegion);
            }

            // Финальный прогресс
            ParseProgressChanged?.Invoke(100.0);
        }

        private void ParseComment(string line, ref GeometryRegion regionType, ref double beamDiameter,
                                  ref double hatchDistance, ref double hatchAngle)
        {
            var match = _commentConfigRegex.Match(line);
            if (!match.Success)
                return;

            string key = match.Groups[1].Value.ToUpper();
            string value = match.Groups[2].Value.Trim();

            switch (key)
            {
                case "MATERIAL":
                    if (!Project.Configuration.ContainsKey("material"))
                    {
                        Project.Configuration.AddParameter("material").SetValue(value);
                    }
                    break;

                case "LAYER_HEIGHT":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double layerHeight))
                    {
                        if (!Project.Configuration.ContainsKey("layer_height"))
                        {
                            Project.Configuration.AddParameter("layer_height").SetValue(layerHeight);
                        }
                    }
                    break;

                case "PROJECT_NAME":
                    Project.ProjectInfo.Name = value;
                    break;

                case "REGION_TYPE":
                    if (CncSyntax.RegionTypeMap.TryGetValue(value, out var region))
                    {
                        regionType = region;
                    }
                    break;

                case "BEAM_DIAMETER":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double beam))
                    {
                        beamDiameter = beam;
                    }
                    break;

                case "HATCH_DISTANCE":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double hatch))
                    {
                        hatchDistance = hatch;
                    }
                    break;

                case "HATCH_ANGLE":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double angle))
                    {
                        hatchAngle = angle;
                    }
                    break;
            }
        }

        private Dictionary<char, float> ParseParameters(string line)
        {
            var parameters = new Dictionary<char, float>();
            var matches = _parameterRegex.Matches(line);

            foreach (Match match in matches)
            {
                char param = match.Groups[1].Value[0];
                if (float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    parameters[param] = value;
                }
            }

            return parameters;
        }

        private void UpdatePosition(Dictionary<char, float> parameters, ref float x, ref float y, ref float z)
        {
            if (parameters.ContainsKey('X'))
                x = parameters['X'];

            if (parameters.ContainsKey('Y'))
                y = parameters['Y'];

            if (parameters.ContainsKey('Z'))
                z = parameters['Z'];
        }

        private Layer GetOrCreateLayer(int layerId)
        {
            var layer = Project.Layers.FirstOrDefault(l => l.Id == layerId);
            if (layer == null)
            {
                layer = new Layer
                {
                    Id = layerId,
                    Regions = new List<RegionModel>()
                };
                Project.Layers.Add(layer);
            }
            return layer;
        }

        private ProjectParserTest.Parsers.Shared.Models.Region CreateRegion(GeometryRegion regionType, double power, double speed,
                                   double beamDiameter, double hatchDistance, double hatchAngle)
        {
            var part = Project.GetParts().FirstOrDefault() ?? new Part { Id = 0, Name = "Default Part" };

            return new ProjectParserTest.Parsers.Shared.Models.Region
            {
                LaserNum = 0,
                Part = part,
                ExposeLength = 0,
                Type = BlockType.PolyLine,
                PolyLines = new List<PolyLine>(),
                GeometryRegion = regionType,
                Parameters = new RegionParameters
                {
                    LaserPower = power,
                    LaserSpeed = speed,
                    LaserBeamDiameter = beamDiameter,
                    HatchDistance = hatchDistance,
                    Angle = hatchAngle,
                    Skywriting = 0
                }
            };
        }

        public Task<Layer?> GetLayer(int index) => Task.FromResult(Project.Layers.ElementAtOrDefault(index));

        public Task<Layer> GetLayer() => Task.FromResult(Project.CurrentLayer);

        /// <summary>
        /// Освобождает ресурсы проекта из памяти
        /// </summary>
        public void ClearProject()
        {
            if (Project == null) return;

            try
            {
                // Очищаем слои и их регионы
                if (Project.Layers != null)
                {
                    foreach (var layer in Project.Layers)
                    {
                        if (layer?.Regions != null)
                        {
                            foreach (var region in layer.Regions)
                            {
                                // Очищаем полилинии
                                if (region.PolyLines != null)
                                {
                                    foreach (var polyline in region.PolyLines)
                                    {
                                        polyline?.Points?.Clear();
                                    }
                                    region.PolyLines.Clear();
                                }
                            }
                            layer.Regions.Clear();
                        }
                    }
                    Project.Layers.Clear();
                }

                // Очищаем конфигурацию и заголовки
                Project.HeaderInfo?.DataList?.Clear();
                Project.Configuration?.DataList?.Clear();

                // Обнуляем ссылки
                Project.CurrentLayer = null;
                Project.ProjectInfo = null;
                Project = null;

                // Сбрасываем счетчики
                _totalLinesCount = 0;
                _currentLineNumber = 0;

                Console.WriteLine("[CncProvider] Project resources cleared from memory");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CncProvider.ClearProject] Error: {ex.Message}");
            }
        }
    }
}
