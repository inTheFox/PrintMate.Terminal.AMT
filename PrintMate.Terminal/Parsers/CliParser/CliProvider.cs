using DryIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrintMate.Terminal.Parsers.Shared;
using PrintMate.Terminal.Parsers.Shared.Models;
using Prism.Events;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Interfaces;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTK.Graphics.ES11;
using static ImTools.ImMap;
using static PrintMate.Terminal.Parsers.CliParser.CliSyntax;

namespace ProjectParserTest.Parsers.CliParser
{
    public class CliProvider : IParserProvider
    {
        private static Regex _userdataRegex = new Regex(@"^([^,]+),([^,]+),(\{.*\})$");
        private static Regex _labelRegex = new Regex(@"^(\d+),""([^""]*)""$");
        private static Regex _headerParameterKeyPairValueRegex = new Regex(@"^\$\$([^/]+)/(.*)$");

        public Project Project { get; set; } = null;
        public event Action<string> ParseStarted;
        public event Action<Project> ParseCompleted;
        public event Action<string> ParseError;
        public event Action<double> ParseProgressChanged;

        private int _laser1RegionIdCounter = 0;
        private int _laser2RegionIdCounter = 0;


        private double _totalLayersCount = 0;

        public CliProvider()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public Task NextLayer()
        {
            var nextLayer = Project.Layers.ElementAtOrDefault(Project.CurrentLayer.Id + 1);
            if (nextLayer != null)
            {
                Project.CurrentLayer = nextLayer;
                //Console.WriteLine($"\nNEXT LAYER ID {nextLayer.Id}\n");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Легкий парсинг CLI файла - только заголовок и конфигурация, без геометрии.
        /// Используется для импорта проекта в БД без полного анализа геометрии.
        /// </summary>
        public async Task<Project> ParseHeaderOnlyAsync(string path)
        {
            try
            {
                ParseStarted?.Invoke(path);

                Project = new Project
                {
                    ProjectInfo = new ProjectInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(path),
                        Path = path,
                    },
                    Layers = new List<Layer>(),
                    HeaderInfo = new Data(),
                    Configuration = new Data(),
                };

                byte[] fileBytes = await File.ReadAllBytesAsync(path);
                ParseHeader(fileBytes, out int geometryStartIndex);

                // Считаем количество слоёв без парсинга геометрии (быстрый подсчёт тегов)
                int layersCount = CountLayersInBinaryGeometry(fileBytes, geometryStartIndex);

                // Устанавливаем количество слоёв в HeaderInfo для вычисления высоты проекта
                if (!Project.HeaderInfo.ContainsKey(HeaderKeys.Info.LayersCount))
                {
                    Project.HeaderInfo.AddParameter(HeaderKeys.Info.LayersCount).SetValue(layersCount);
                }

                // Заполняем ProjectInfo из заголовка и конфигурации
                float layerThickness = Project.GetLayerThicknessInMillimeters();
                Project.ProjectInfo.ProjectHeight = layerThickness * layersCount;
                Project.ProjectInfo.LayerSliceHeight = layerThickness;
                Project.ProjectInfo.ProjectLink = Project;
                Project.ProjectInfo.MaterialName = Project.GetMaterialName();
                Project.ProjectInfo.PrintTime = "N/A"; // Время печати недоступно без полного парсинга

                ParseProgressChanged?.Invoke(100);
                ParseCompleted?.Invoke(Project);

                Console.WriteLine($"[CliProvider] Header-only parsing completed: {layersCount} layers, height={Project.ProjectInfo.ProjectHeight}mm");
                return Project;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[CliProvider] ParseHeaderOnlyAsync error: {e.Message}");
                ParseError?.Invoke(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Быстрый подсчёт слоёв в бинарной геометрии без полного парсинга
        /// </summary>
        private int CountLayersInBinaryGeometry(byte[] fileBytes, int startIndex)
        {
            int layersCount = 0;
            int geometrySize = fileBytes.Length - startIndex;

            if (geometrySize <= 0) return 0;

            using var ms = new MemoryStream(fileBytes, startIndex, geometrySize);
            using var reader = new BinaryReader(ms, Encoding.ASCII, true);

            while (ms.Position < ms.Length - 1)
            {
                try
                {
                    short tag = reader.ReadInt16();

                    // Считаем только теги слоёв
                    if (tag == GEOMETRY_LAYER_LONG_BINARY_TAG)
                    {
                        layersCount++;
                        reader.ReadSingle(); // Пропускаем высоту слоя (float)
                    }
                    else if (tag == GEOMETRY_LAYER_SHORT_BINARY_TAG)
                    {
                        layersCount++;
                        reader.ReadInt16(); // Пропускаем высоту слоя (short)
                    }
                    // Пропускаем геометрию полилиний
                    else if (tag == GEOMETRY_POLYLINE_SHORT_BINARY_TAG)
                    {
                        reader.ReadInt16(); // id
                        reader.ReadInt16(); // type
                        int points = reader.ReadInt16();
                        ms.Position += points * 4; // 2 shorts per point
                    }
                    else if (tag == GEOMETRY_POLYLINE_LONG_BINARY_TAG)
                    {
                        reader.ReadInt32(); // id
                        reader.ReadInt32(); // type
                        int points = reader.ReadInt32();
                        ms.Position += points * 8; // 2 floats per point
                    }
                    else if (tag == GEOMETRY_POLYLINE_INT_BINARY_TAG)
                    {
                        reader.ReadInt32(); // id
                        reader.ReadInt32(); // type
                        int points = reader.ReadInt32();
                        ms.Position += points * 8; // 2 ints per point
                    }
                    // Пропускаем геометрию штриховок
                    else if (tag == GEOMETRY_HATCHES_SHORT_BINARY_TAG)
                    {
                        reader.ReadInt16(); // id
                        int points = reader.ReadInt16();
                        ms.Position += points * 8; // 4 shorts per hatch line
                    }
                    else if (tag == GEOMETRY_HATCHES_LONG_BINARY_TAG)
                    {
                        reader.ReadInt32(); // id
                        int points = reader.ReadInt32();
                        ms.Position += points * 16; // 4 floats per hatch line
                    }
                    else if (tag == GEOMETRY_HATCHES_INT_BINARY_TAG)
                    {
                        reader.ReadInt32(); // id
                        int points = reader.ReadInt32();
                        ms.Position += points * 16; // 4 ints per hatch line
                    }
                    else
                    {
                        // Неизвестный тег - прерываем, чтобы избежать бесконечного цикла
                        break;
                    }
                }
                catch
                {
                    // Ошибка чтения - выходим
                    break;
                }
            }

            return layersCount;
        }

        public async Task<Project> ParseAsync(string path)
        {
            try
            {
                // Пушим событие начала парсинга
                ParseStarted?.Invoke(path);

                // Создаем проект
                Project = new Project
                {
                    ProjectInfo = new ProjectInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(path),
                        Path = path,
                    },
                    Layers = new List<Layer>(),
                    HeaderInfo = new Data(),
                    Configuration = new Data(),
                };

                byte[] fileBytes = await File.ReadAllBytesAsync(path);
                ParseHeader(fileBytes, out int geometryStartIndex);

                // Репортим прогресс после парсинга заголовка
                double headerProgress = ((double)geometryStartIndex / fileBytes.Length) * 100.0;
                ParseProgressChanged?.Invoke(headerProgress);

                float unitsMultiplier = 1f;
                if (Project.HeaderInfo.ContainsKey(HeaderKeys.Info.UnitsParameterKey))
                    Project.HeaderInfo.GetParameter(HeaderKeys.Info.UnitsParameterKey)?.GetValue(ref unitsMultiplier);

                int fileVersion = 0;
                if (Project.HeaderInfo.ContainsKey(HeaderKeys.Info.VersionParameterKey))
                    Project.HeaderInfo.GetParameter(HeaderKeys.Info.VersionParameterKey)?.GetValue(ref fileVersion);

                bool isBinary = this.IsBinaryGeometry(fileBytes, geometryStartIndex);
                if (isBinary) ParseBinaryGeometry(fileBytes, geometryStartIndex, fileVersion, unitsMultiplier);
                else this.ParseAsciiGeometry(fileBytes, geometryStartIndex, unitsMultiplier);

                // Удаляем слои где вообще нет регионов
                Project.Layers.RemoveAll(p => p.Regions.Count <= 0);

                // Теперь пересчитываем ID
                for (int i = 0; i < Project.Layers.Count; i++)
                {
                    Project.Layers[i].Id = i + 1;
                    Project.Layers[i].AbsoluteId = i;
                }

                // Если список деталей пуст, создаем деталь по умолчанию
                if (Project.HeaderInfo.ContainsKey(HeaderKeys.Info.Parts))
                {
                    var partsList = Project.HeaderInfo.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
                    if (partsList == null || partsList.Count == 0)
                    {
                        var defaultPart = new Part
                        {
                            Id = 0,
                            Name = "Деталь"
                        };
                        Project.HeaderInfo.GetParameter(HeaderKeys.Info.Parts).SetValue(new List<Part> { defaultPart });
                        //Console.WriteLine($"[CliProvider] No parts found, created default part: {defaultPart.Name}");
                    }
                }



                Project.ProjectInfo.ProjectHeight = Project.GetProjectHeight();
                Project.ProjectInfo.LayerSliceHeight = Project.GetLayerThicknessInMillimeters();
                Project.ProjectInfo.ProjectLink = Project;
                Project.ProjectInfo.MaterialName = Project.GetMaterialName();
                Project.ProjectInfo.PrintTime = Project.GetPrintTimeFormatted();

                if (Project.Layers.Count > 0)
                {
                    Project.CurrentLayer = Project.Layers[0];
                }
                else
                {
                    Console.WriteLine("[WARNING] No layers found after parsing!");
                }
                ////Console.WriteLine(Project.CurrentLayer);
                ParseCompleted?.Invoke(Project);

                return Project;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
        private void ParseHeader(byte[] fileBytes, out int geometryStartIndex)
        {
            try
            {
                geometryStartIndex = 0;

                string headerText = Encoding.ASCII.GetString(fileBytes);
                int headerEndPos = headerText.IndexOf(HeaderKeys.Info.ParameterStartParameterKey + HeaderKeys.Info.HeaderEndParameterKey, StringComparison.OrdinalIgnoreCase);
                if (headerEndPos == -1) throw new Exception(HeaderKeys.Info.HeaderEndParameterKey + " not found.");
                string headerPart = headerText.Substring(0, headerEndPos + (HeaderKeys.Info.ParameterStartParameterKey + HeaderKeys.Info.HeaderEndParameterKey).Length);

                Project.HeaderInfo.AddParameter(HeaderKeys.Info.Parts).SetValue(new List<Part>());

                using (var reader = new StringReader(headerPart))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith(HeaderKeys.Info.ParameterStartParameterKey))
                        {
                            if (_headerParameterKeyPairValueRegex.IsMatch(line))
                            {
                                Match headerKeyPairValueMatch = _headerParameterKeyPairValueRegex.Match(line);
                                string parameterName = headerKeyPairValueMatch.Groups[1].Value;
                                string parameterValue = headerKeyPairValueMatch.Groups[2].Value;

                                switch (parameterName)
                                {
                                    case HeaderKeys.Info.VersionParameterKey:
                                        if (int.TryParse(parameterValue, out int version))
                                        {
                                            Project.HeaderInfo.AddParameter(HeaderKeys.Info.VersionParameterKey).SetValue(version);
                                            ////Console.WriteLine($"Parameter: {parameterName}, Value: {version}");
                                        }
                                        break;
                                    case HeaderKeys.Info.UnitsParameterKey:
                                        Project.HeaderInfo.AddParameter(HeaderKeys.Info.UnitsParameterKey).SetValue(float.Parse(parameterValue, CultureInfo.InvariantCulture));
                                        break;
                                    case HeaderKeys.Info.LayersParameterKey:
                                        if (int.TryParse(parameterValue, out int layersCount))
                                        {
                                            _totalLayersCount = layersCount;
                                            Project.HeaderInfo.AddParameter(HeaderKeys.Info.LayersParameterKey).SetValue(layersCount);
                                        }
                                        break;
                                    case HeaderKeys.Info.LasersParameterKey:
                                        if (int.TryParse(parameterValue, out int lasersCount))
                                        {
                                            Project.HeaderInfo.AddParameter(HeaderKeys.Info.LasersParameterKey).SetValue(lasersCount);
                                        }
                                        break;
                                    case HeaderKeys.Info.MachineParameterKey:
                                        Project.HeaderInfo.AddParameter(HeaderKeys.Info.MachineParameterKey).SetValue(parameterValue);
                                        break;
                                    case HeaderKeys.Info.LabelParameterKey:
                                        if (_labelRegex.IsMatch(parameterValue))
                                        {
                                            var labelMatch = _labelRegex.Match(parameterValue);
                                            string partIdString = labelMatch.Groups[1].Value;
                                            string partName = labelMatch.Groups[2].Value;
                                            if (int.TryParse(labelMatch.Groups[1].Value, out int partId))
                                            {
                                                Project.HeaderInfo.
                                                    GetParameter(HeaderKeys.Info.Parts)
                                                    .GetValue<List<Part>>()
                                                    .Add(new Part { Id = Project.GetPartId(partId), Name = partName });
                                                ////Console.WriteLine($"Деталь: {partName}, Id: {Project.GetPartId(partId)}");
                                            }
                                        }
                                        break;
                                    case HeaderKeys.Info.UserdataParameterKey:
                                        if (_userdataRegex.IsMatch(parameterValue))
                                        {
                                            var userdataMatch = _userdataRegex.Match(parameterValue);

                                            Project.HeaderInfo.AddParameter(HeaderKeys.Info.UserdataParameterKey).SetValue(new string[]
                                            {
                                                userdataMatch.Groups[1].Value,
                                                userdataMatch.Groups[2].Value,
                                                userdataMatch.Groups[3].Value
                                            });
                                            ////Console.WriteLine($"Parameter: {parameterName}, Values: ({userdataMatch.Groups[1].Value}, {userdataMatch.Groups[2].Value}, {userdataMatch.Groups[3].Value})");
                                        }
                                        break;
                                }
                            }
                            

                        }
                        else if (line.StartsWith("{"))
                        {
                            ////Console.WriteLine(JsonConvert.SerializeObject(Project.HeaderInfo.GetList(), Formatting.Indented));


                            try
                            {
                                HandleJsonConfiguration(line);
                            }
                            catch (Exception e)
                            {
                                //Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                }

                geometryStartIndex = Encoding.ASCII.GetByteCount(headerPart);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                throw;
            }
        }
        private void HandleJsonConfiguration(string json)
        {
            try
            {
                JObject jRoot = JObject.Parse(json);


                foreach (var field in typeof(HeaderKeys.Settings).GetFields())
                {
                    string path = field.GetValue(null) as string;

                    JToken token = jRoot.SelectToken("base." + path);
                    if (token == null || token.Type == JTokenType.Null)
                    {
                        ////Console.WriteLine($"Параметр '{path}' не найден в JSON.");
                        continue;
                    }

                    var parameter = Project.Configuration.AddParameter(path);

                    switch (token.Type)
                    {
                        case JTokenType.String:
                            string value = token.ToString();

                            if (int.TryParse(value, out int intResult))
                            {
                                parameter.SetValue(intResult);
                            }
                            else if (double.TryParse(value, out double doubleResult))
                            {
                                parameter.SetValue(doubleResult);
                            }
                            else if (float.TryParse(value, out float floatResult))
                            {
                                parameter.SetValue(floatResult);
                            }
                            else if (bool.TryParse(value, out bool booleanResult))
                            {
                                parameter.SetValue(booleanResult);
                            }
                            else
                            {
                                parameter.SetValue(value);
                            }
                            break;
                        case JTokenType.Array:
                            switch (field.Name)
                            {
                                case nameof(HeaderKeys.Settings.BuildOrder):
                                    var buildOrderList = token.ToObject<List<string>>();
                                    parameter.SetValue(buildOrderList);

                                    Project.HeaderInfo.AddParameter(HeaderKeys.Info.LayersCount)
                                        .SetValue(buildOrderList.Count);
                                    break;
                            }
                            break;
                    }
                }

                if (Project.HeaderInfo.ContainsKey(HeaderKeys.Info.Parts) && 
                    Project.HeaderInfo.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts).Count > 0)
                {
                    var parts = Project.HeaderInfo.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);

                    foreach (var prop in jRoot.Properties())
                    {
                        string key = prop.Name;
                        if (key == "base" || key == "etb")
                            continue;
                        if (int.TryParse(key, out int id))
                        {
                            id = Project.GetPartId(id);
                            var part = parts.FirstOrDefault(p => p.Id == id);
                            if (part != null)
                            {
                                if (prop.Value is JObject section)
                                {
                                    foreach (var subProp in section.Properties())
                                    {
                                        string fullPath = $"{key}.{subProp.Name}";
                                        string value = subProp.Value.ToString();

                                        if (int.TryParse(value, out int intResult))
                                            part.Data.AddParameter(subProp.Name).SetValue(intResult);
                                        else if (double.TryParse(value, out double doubleResult))
                                            part.Data.AddParameter(subProp.Name).SetValue(doubleResult);
                                        else if (bool.TryParse(value, out bool boolResult))
                                            part.Data.AddParameter(subProp.Name).SetValue(boolResult);
                                        else
                                            part.Data.AddParameter(subProp.Name).SetValue(value);

                                        ////Console.WriteLine($"PartId: {part.Id}, PartName: {part.Name}, Key: {subProp.Name}, Value: {value}");
                                    }

                                }
                            }
                        }
                    }

                }

                ////Console.WriteLine(JsonConvert.SerializeObject(Project.Configuration.GetList(), Formatting.Indented));
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                throw;
            }
            
        }
        private void ParseBinaryGeometry(byte[] fileBytes, int startIndex, int fileVersion, float unitsMultiplier)
        {
            try
            {
                int layerCounter = 1;
                Layer currentLayer = null;
                long lastProgressUpdate = 0;
                long totalFileSize = fileBytes.Length;
                int geometrySize = fileBytes.Length - startIndex;

                using (var ms = new MemoryStream(fileBytes, startIndex, geometrySize))
                using (var reader = new BinaryReader(ms, Encoding.ASCII, true))
                {

                    bool hasData = false;
                    int tagCount = 0;
                    while (ms.Position < ms.Length)
                    {
                        short geometry = reader.ReadInt16();

                        // Логируем только первые 5 тегов и каждый 100-й для отладки
                        if (tagCount < 5 || tagCount % 100 == 0)
                        {
                            //Console.WriteLine($"[DEBUG #{tagCount}] Position: {ms.Position}, Read tag: {geometry} (0x{geometry:X})");
                        }
                        tagCount++;

                        hasData = true;

                        // Обновляем прогресс каждые 1% чтения файла
                        // Учитываем, что заголовок уже был прочитан (startIndex байт)
                        if (ms.Position - lastProgressUpdate > geometrySize / 100)
                        {
                            lastProgressUpdate = ms.Position;
                            // Рассчитываем прогресс относительно всего файла
                            double bytesProcessed = startIndex + ms.Position;
                            double progress = (bytesProcessed / totalFileSize) * 100.0;
                            ParseProgressChanged?.Invoke(progress);
                        }

                        // Новый слой
                        if (geometry == GEOMETRY_LAYER_LONG_BINARY_TAG || geometry == GEOMETRY_LAYER_SHORT_BINARY_TAG)
                        {
                            // Добавляем предыдущий слой
                            if (currentLayer != null)
                            {
                                Project.Layers.Add(currentLayer);
                            }

                            // НЕ обновляем прогресс здесь - используем только позицию в файле для единообразия

                            // Создаем новый
                            currentLayer = new Layer();
                            currentLayer.Id = layerCounter; ;
                            currentLayer.Regions = new List<Region>();
                            layerCounter++;

                            //Console.WriteLine("NEW LAYER");


                            // Обрабатываем высоту слоя
                            if (geometry == GEOMETRY_LAYER_LONG_BINARY_TAG)
                            {
                                float layerHeight = reader.ReadSingle() * unitsMultiplier;
                                currentLayer.Height = layerHeight;
                                //Console.WriteLine($"Layer height (LONG): {layerHeight}");
                            }

                            if (geometry == GEOMETRY_LAYER_SHORT_BINARY_TAG)
                            {
                                short layerHeightShort = reader.ReadInt16();
                                float layerHeight = layerHeightShort * unitsMultiplier;
                                currentLayer.Height = layerHeight;
                                //Console.WriteLine($"Layer height (SHORT): {layerHeight}");
                            }
                        }

                        #region Polylines
                        if (geometry == GEOMETRY_POLYLINE_SHORT_BINARY_TAG ||
                            geometry == GEOMETRY_POLYLINE_LONG_BINARY_TAG ||
                            geometry == GEOMETRY_POLYLINE_INT_BINARY_TAG)
                        {
                            int id;
                            int type;
                            int pointsCount;
                            float prevX = 0;
                            float prevY = 0;
                            bool first = true;

                            if (geometry == GEOMETRY_POLYLINE_SHORT_BINARY_TAG)
                            {
                                id = reader.ReadInt16();
                                type = reader.ReadInt16();
                                pointsCount = reader.ReadInt16();
                            }
                            else
                            {
                                id = reader.ReadInt32();
                                type = reader.ReadInt32();
                                pointsCount = reader.ReadInt32();
                            }

                            DecodedInfo info = this.GetDecodedInfo(id, type == 2, true, fileVersion);
                            PolyLine polyLine = new PolyLine();

                            Region region = new Region
                            {
                                LaserNum = info.LaserNum,
                                Part = Project.GetPartById(id)!,
                                ExposeLength = 0,
                                Type = BlockType.PolyLine,
                                PolyLines = new List<PolyLine>(),
                                Parameters = GetRegionParameters(info.Region, info),
                                GeometryRegion = info.Region
                            };
                            if (region.LaserNum == 0)
                            {
                                region.Id = _laser1RegionIdCounter++;
                            }
                            else
                            {
                                region.Id = _laser2RegionIdCounter++;
                            }

                            //Console.WriteLine($"{(CliTag)geometry}, {info.ToString()}, {region.Parameters.ToString()}");

                            for (int i = 0; i < pointsCount; i++)
                            {
                                float x = 0;
                                float y = 0;

                                if (geometry == GEOMETRY_POLYLINE_SHORT_BINARY_TAG)
                                {
                                    x = reader.ReadInt16() * unitsMultiplier;
                                    y = reader.ReadInt16() * unitsMultiplier;
                                }
                                else if (geometry == GEOMETRY_POLYLINE_LONG_BINARY_TAG)
                                {
                                    x = reader.ReadSingle() * unitsMultiplier;
                                    y = reader.ReadSingle() * unitsMultiplier;
                                }
                                else if (geometry == GEOMETRY_POLYLINE_INT_BINARY_TAG)
                                {
                                    x = reader.ReadInt32() * unitsMultiplier;
                                    y = reader.ReadInt32() * unitsMultiplier;
                                }

                                polyLine.Add(new Point(x, y));

                                if (!first)
                                {
                                    double dist = Math.Sqrt(Math.Pow(x - prevX, 2) + Math.Pow(y - prevY, 2));
                                    region.ExposeLength += dist;
                                }

                                prevX = x;
                                prevY = y;
                                first = false;
                            }

                            region.PolyLines.Add(polyLine);
                            currentLayer?.Regions.Add(region);
                        }

                        #endregion
                        #region Hatches
                        if (geometry == GEOMETRY_HATCHES_INT_BINARY_TAG ||
                            geometry == GEOMETRY_HATCHES_LONG_BINARY_TAG ||
                            geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG)
                        {
                            int id = 0;
                            int points = 0;

                            if (geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG)
                            {
                                id = reader.ReadInt16();
                                points = reader.ReadInt16();
                            }
                            else
                            {
                                id = reader.ReadInt32();
                                points = reader.ReadInt32();
                            }

                            DecodedInfo info = this.GetDecodedInfo(id, false, false, fileVersion);
                            Region region = new Region
                            {
                                LaserNum = info.LaserNum,
                                Part = Project.GetPartById(id)!,
                                ExposeLength = 0,
                                PolyLines = new List<PolyLine>(),
                                Parameters = GetRegionParameters(info.Region, info),
                                GeometryRegion = info.Region
                            };

                            //Console.WriteLine($"{(CliTag)geometry}, {info.ToString()}, {region.Parameters.ToString()}");

                            for (int i = 0; i < points; i++)
                            {
                                var polyLine = new PolyLine();

                                float x1 = 0;
                                float y1 = 0;
                                float x2 = 0;
                                float y2 = 0;

                                if (geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG) x1 = reader.ReadInt16() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG) y1 = reader.ReadInt16() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG) x2 = reader.ReadInt16() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_SHORT_BINARY_TAG) y2 = reader.ReadInt16() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_LONG_BINARY_TAG) x1 = reader.ReadSingle() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_LONG_BINARY_TAG) y1 = reader.ReadSingle() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_LONG_BINARY_TAG) x2 = reader.ReadSingle() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_LONG_BINARY_TAG) y2 = reader.ReadSingle() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_INT_BINARY_TAG) x1 = reader.ReadInt32() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_INT_BINARY_TAG) y1 = reader.ReadInt32() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_INT_BINARY_TAG) x2 = reader.ReadInt32() * unitsMultiplier;
                                if (geometry == GEOMETRY_HATCHES_INT_BINARY_TAG) y2 = reader.ReadInt32() * unitsMultiplier;

                                polyLine.Add(new Point(x1, y1));
                                polyLine.Add(new Point(x2, y2));
                                region.PolyLines.Add(polyLine);

                                double dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                                region.ExposeLength += dist;
                            }
                            currentLayer?.Regions.Add(region);

                        }
                        #endregion

                        // Неизвестный тег - логируем
                        if (geometry != GEOMETRY_LAYER_LONG_BINARY_TAG &&
                            geometry != GEOMETRY_LAYER_SHORT_BINARY_TAG &&
                            geometry != GEOMETRY_POLYLINE_SHORT_BINARY_TAG &&
                            geometry != GEOMETRY_POLYLINE_LONG_BINARY_TAG &&
                            geometry != GEOMETRY_POLYLINE_INT_BINARY_TAG &&
                            geometry != GEOMETRY_HATCHES_SHORT_BINARY_TAG &&
                            geometry != GEOMETRY_HATCHES_LONG_BINARY_TAG &&
                            geometry != GEOMETRY_HATCHES_INT_BINARY_TAG)
                        {
                            Console.WriteLine($"[WARNING] Unknown geometry tag: {geometry} (0x{geometry:X}) at position {ms.Position - 2}");
                        }
                    }
                }

                // Добавляем последний слой
                if (currentLayer != null)
                {
                    Project.Layers.Add(currentLayer);
                }

                Console.WriteLine($"[PARSING SUMMARY]");
                    //Console.WriteLine($"  Total tags read: {tagCount}");
                Console.WriteLine($"  Total layers created: {layerCounter - 1}");
                Console.WriteLine($"  Layers with regions: {Project.Layers.Count(l => l.Regions.Count > 0)}");
                Console.WriteLine($"  Empty layers: {Project.Layers.Count(l => l.Regions.Count == 0)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public Task<Layer?> GetLayer(int layerId) => Task.FromResult(Project.Layers.FirstOrDefault(p=>p.Id == layerId));
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
                _laser1RegionIdCounter = 0;
                _laser2RegionIdCounter = 0;
                _totalLayersCount = 0;

                Console.WriteLine("[CliProvider] Project resources cleared from memory");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CliProvider.ClearProject] Error: {ex.Message}");
            }
        }

        public T GetRegionParameterFromConfiguration<T>(string key, DecodedInfo decodedInfo)
        {
            var part = Project.GetParts().FirstOrDefault(p => p.Id == decodedInfo.PartId);
            if (part != null)
            {
                if (part.Data.ContainsKey(key))
                {
                    T value = (T)Convert.ChangeType(part.Data.GetParameter(key).GetValue(), typeof(T));
                    ////Console.WriteLine($"Key: {key} = {value.ToString()} //");
                    return value;
                }
            }
            ////Console.WriteLine($"Key: {key} = {Project.Configuration.GetParameterValue<T>(key)}");
            return Project.Configuration.GetParameterValue<T>(key);
        }

        public RegionParameters GetRegionParameters(GeometryRegion region, DecodedInfo decodedInfo)
        {
            RegionParameters parameters = new RegionParameters();
            switch (region)
            {
                case GeometryRegion.Edges:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.EdgeLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.EdgeLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.EdgeLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.EdgeSkywriting, decodedInfo);
                    break;

                case GeometryRegion.Contour:
                case GeometryRegion.None:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillContourLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillContourLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillContourLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillContourSkywriting, decodedInfo);
                    break;

                case GeometryRegion.Infill:
                case GeometryRegion.InfillRegionPreview:
                    // Infill поддерживает параметры, зависящие от высоты слоя
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillHatchLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillHatchLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillHatchLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillHatchSkywriting, decodedInfo);
                    parameters.HatchDistance = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillHatchDistance, decodedInfo);
                    parameters.Angle = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.InfillAngle, decodedInfo);
                    break;

                case GeometryRegion.ContourUpskin:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinContourLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinContourLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinContourLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinContourSkywriting, decodedInfo);
                    break;

                case GeometryRegion.Upskin:
                case GeometryRegion.UpskinRegionPreview:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinHatchLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinHatchLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinHatchLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinHatchSkywriting, decodedInfo);
                    parameters.HatchDistance = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinHatchDistance, decodedInfo);
                    parameters.Angle = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.UpskinAngle, decodedInfo);
                    break;

                case GeometryRegion.ContourDownskin:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinBorderLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinBorderLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinBorderLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinBorderSkywriting, decodedInfo);
                    break;

                case GeometryRegion.Downskin:
                case GeometryRegion.DownskinRegionPreview:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinHatchLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinHatchLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinHatchLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinHatchSkywriting, decodedInfo);
                    parameters.HatchDistance = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinHatchDistance, decodedInfo);
                    parameters.Angle = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.DownskinAngle, decodedInfo);
                    break;

                case GeometryRegion.Support:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportBorderLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportBorderLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportBorderLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportBorderSkywriting, decodedInfo);
                    break;

                case GeometryRegion.SupportFill:
                    parameters.LaserBeamDiameter = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportHatchLaserBeamDiameter, decodedInfo);
                    parameters.LaserPower = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportHatchLaserPower, decodedInfo);
                    parameters.LaserSpeed = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportHatchLaserSpeed, decodedInfo);
                    parameters.Skywriting = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SupportHatchSkywriting, decodedInfo);
                    parameters.HatchDistance = GetRegionParameterFromConfiguration<double>(HeaderKeys.Settings.SolidSupportHatchDistance, decodedInfo);
                    break;

                default:
                    //Console.WriteLine($"WARNING: Unknown region type: {region}");
                    break;
            }
            return parameters;
        }
    }
}
