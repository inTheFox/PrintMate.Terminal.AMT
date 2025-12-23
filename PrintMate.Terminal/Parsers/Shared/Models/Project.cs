using System;
using System.Collections.Generic;
using System.Linq;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.CliParser;

namespace ProjectParserTest.Parsers.Shared.Models
{
    public class Project
    {
        public ProjectInfo ProjectInfo { get; set; }
        public Data HeaderInfo { get; set; } = new Data();
        public Data Configuration { get; set; } = new Data();
        public List<Layer> Layers { get; set; } = new List<Layer>();
        public Layer CurrentLayer { get; set; }
        public int CurrentLayerIndex = 0;

        public bool IsLastLayer()
        {
            return CurrentLayer == Layers.Last();
        }

        public Layer NextLayer()
        {
            Console.WriteLine($"\n\n\n============== NEXT LAYER ================\n\n\n");

            var nextLayer = Layers.FirstOrDefault(p=> p.Id == (CurrentLayer.Id + 1));
            if (nextLayer == null) return null;
            CurrentLayer = nextLayer;
            Console.WriteLine($"============== CURRENT LAYER INDEX: {CurrentLayerIndex}, ID: {CurrentLayer.Id} ================");
            CurrentLayerIndex++;
            Console.WriteLine($"\n\n\n===========================================\n\n\n");
            return nextLayer;
        }

        public Layer? GetLayerById(int layerId)
        {
            return Layers?.FirstOrDefault(p => p.Id == layerId);
        }

        public void SetActiveLayer(Layer layer)
        {
            CurrentLayer = layer;
        }

        public ushort GetLayersCount()
        {
            if (Layers == null) return 0;
            return (ushort)Layers.Count;
        }

        public float GetProjectHeight()
        {
            return GetLayerThicknessInMillimeters() * Layers.Count;
        }

        public float GetLayerThicknessInMicrons()
        {
            // Для CLI файлов
            if (Configuration?.ContainsKey(HeaderKeys.Settings.SliceThickness) == true)
            {
                return Configuration.GetParameter(HeaderKeys.Settings.SliceThickness).GetValue<int>();
            }
            // Для CNC файлов - значение по умолчанию
            return 50; // 50 микрон
        }

        public float GetLayerThicknessInMillimeters()
        {
            // Для CLI файлов
            if (Configuration?.ContainsKey(HeaderKeys.Settings.SliceThickness) == true)
            {
                return Configuration.GetParameter(HeaderKeys.Settings.SliceThickness).GetValue<int>() / 1000f;
            }
            // Для CNC файлов - проверяем layer_height
            if (Configuration?.ContainsKey("layer_height") == true)
            {
                return (float)Configuration.GetParameterValue<double>("layer_height");
            }
            // Значение по умолчанию
            return 0.05f;
        }

        public List<Part> GetParts()
        {
            if (HeaderInfo?.ContainsKey(HeaderKeys.Info.Parts) == true)
            {
                return HeaderInfo.GetParameter(HeaderKeys.Info.Parts)?.GetValue<List<Part>>() ?? new List<Part>();
            }
            return new List<Part>();
        }

        public string GetMaterialName()
        {
            // Для CLI файлов
            if (Configuration?.ContainsKey(HeaderKeys.Settings.MaterialName) == true)
            {
                return Configuration.GetParameter(HeaderKeys.Settings.MaterialName)?.GetValue<string>() ?? "Unknown";
            }
            // Для CNC файлов
            if (Configuration?.ContainsKey("material") == true)
            {
                return Configuration.GetParameterValue<string>("material") ?? "Unknown";
            }
            return "Unknown";
        }

        /// <summary>
        /// Вычислить общее время печати проекта в секундах
        /// </summary>
        public double GetPrintTimeInSeconds()
        {
            if (Layers == null || Layers.Count == 0)
                return 0;

            double totalTime = 0;

            foreach (var layer in Layers)
            {
                if (layer?.Regions == null)
                    continue;

                foreach (var region in layer.Regions)
                {
                    if (region?.Parameters != null && region.Parameters.LaserSpeed > 0)
                    {
                        // Время = расстояние (мм) / скорость (мм/с) = секунды
                        totalTime += region.ExposeLength / region.Parameters.LaserSpeed;
                    }
                }
            }

            return totalTime;
        }

        /// <summary>
        /// Получить время печати в формате "ЧЧ:ММ:СС"
        /// </summary>
        public string GetPrintTimeFormatted()
        {
            double totalSeconds = GetPrintTimeInSeconds();
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            else
            {
                return $"00:{timeSpan.Seconds:D2}";
            }
        }
    }
}
