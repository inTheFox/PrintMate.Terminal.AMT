using Emgu.CV;
using Emgu.CV.CvEnum;
using LayerAnalyzer.Lib.Models.Calibration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace LayerAnalyzer.Lib.Services.Utils
{
    public class CalibrationSettingsLoader
    {
        public bool Save(CalibrationSettings settings, string filePath)
        {
            if (settings == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                var jsonObject = new JObject();

                // Сохраняем Mat-объекты
                jsonObject["cameraMatrix"] = MatToJObject(settings.CameraMatrix);
                jsonObject["distCoeffs"] = MatToJObject(settings.DistCoeffs);
                jsonObject["mapX"] = MatToJObject(settings.MapX);
                jsonObject["mapY"] = MatToJObject(settings.MapY);
                jsonObject["tForm"] = MatToJObject(settings.TForm);

                // Сохраняем ROI
                var roiObject = new JObject();
                roiObject["X"] = settings.Roi.X;
                roiObject["Y"] = settings.Roi.Y;
                roiObject["Width"] = settings.Roi.Width;
                roiObject["Height"] = settings.Roi.Height;
                jsonObject["roi"] = roiObject;

                // Сохраняем Size/SizeF как массивы [width, height]
                jsonObject["frameSizePx"] = new JArray(settings.FrameSizePx.Width, settings.FrameSizePx.Height);
                jsonObject["frameSizeMm"] = new JArray(settings.FrameSizeMm.Width, settings.FrameSizeMm.Height);
                // Сохраняем флаги (опционально, так как их можно восстановить из Mat)
                jsonObject["IsCalibrationValid"] = settings.IsCalibrationValid;
                jsonObject["IsWarpValid"] = settings.IsWarpValid;

                var jsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private JObject MatToJObject(Mat mat)
        {
            if (mat == null || mat.IsEmpty)
            {
                return null;
            }

            var matObj = new JObject();
            matObj["type"] = "cvMap"; // Тип, как в Python-коде
            matObj["data"] = JArray.FromObject(mat.GetData()); // Преобразуем данные Mat в массив
            matObj["height"] = mat.Size.Height;
            matObj["width"] = mat.Size.Width;
            matObj["channels"] = mat.NumberOfChannels;
            matObj["dtype"] = mat.Depth.ToString(); // Например, "Cv32F"
            string cvType = $"CV_{((int)mat.Depth + 1)}C{mat.NumberOfChannels}";
            matObj["cvType"] = cvType;

            return matObj;
        }

        public CalibrationSettings Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Calibration file not found: {filePath}");

            var jsonData = LoadJsonData(filePath);
            return CreateCalibrationSettings(jsonData);
        }

        private static JObject LoadJsonData(string filePath)
        {
            try
            {
                var jsonString = File.ReadAllText(filePath);
                return JObject.Parse(jsonString);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new ArgumentException($"Failed to load or parse JSON file: {ex.Message}", ex);
            }
        }

        private static CalibrationSettings CreateCalibrationSettings(JObject jsonData)
        {
            // Загружаем Mat-объекты
            var cameraMatrix = GetRequiredMat(jsonData, "cameraMatrix", ValidateCameraMatrix);
            var distCoeffs = GetRequiredMat(jsonData, "distCoeffs", ValidateDistCoeffs);
            var mapX = GetRequiredMat(jsonData, "mapX", ValidateMap);
            var mapY = GetRequiredMat(jsonData, "mapY", ValidateMap);
            var tForm = GetRequiredMat(jsonData, "tForm", ValidateTForm);
            var roi = GetRoi(jsonData, "roi");
            // Загружаем Size/SizeF из массивов [width, height]
            var frameSizePx = GetRequiredSize(jsonData, "frameSizePx");
            var frameSizeMm = GetRequiredSizeF(jsonData, "frameSizeMm");
            return new CalibrationSettings(
                cameraMatrix, distCoeffs, mapX, mapY, tForm,
                frameSizePx, frameSizeMm, roi
            );
        }

        private static Rectangle GetRoi(JObject jsonData, string key)
        {
            var rectObject = jsonData[key] as JObject
                             ?? throw new ArgumentException($"Missing required JSON property: {key}");

            // Извлекаем значения из JObject
            int x = rectObject["X"]?.Value<int>() ?? throw new ArgumentException($"Missing 'X' in ROI property: {key}");
            int y = rectObject["Y"]?.Value<int>() ?? throw new ArgumentException($"Missing 'Y' in ROI property: {key}");
            int width = rectObject["Width"]?.Value<int>() ?? throw new ArgumentException($"Missing 'Width' in ROI property: {key}");
            int height = rectObject["Height"]?.Value<int>() ?? throw new ArgumentException($"Missing 'Height' in ROI property: {key}");

            return new Rectangle(x, y, width, height);
        }


        private static Mat GetRequiredMat(JObject jsonData, string key, Func<Mat, bool> validator = null)
        {
            var matObject = jsonData[key] as JObject
                ?? throw new ArgumentException($"Missing required JSON property: {key}");

            var mat = MatFromJObject(matObject)
                ?? throw new ArgumentException($"Invalid Mat data for property: {key}");

            if (validator != null && !validator(mat))
                throw new ArgumentException($"Validation failed for property: {key}");

            return mat;
        }
        private static Mat MatFromJObject(JObject matObj)
        {
            if (matObj == null)
            {
                return null;
            }

            string type = matObj["type"]?.Value<string>();
            if (type != "cvMap")
            {
                throw new ArgumentException($"Invalid Mat type in JSON: {type}");
            }

            var dataJArray = matObj["data"] as JArray ?? throw new ArgumentException("Missing 'data' array in Mat JSON.");
            int height = matObj["height"]?.Value<int>() ?? throw new ArgumentException("Missing 'height' in Mat JSON.");
            int width = matObj["width"]?.Value<int>() ?? throw new ArgumentException("Missing 'width' in Mat JSON.");
            int channels = matObj["channels"]?.Value<int>() ?? throw new ArgumentException("Missing 'channels' in Mat JSON.");
            string dtypeStr = matObj["dtype"]?.Value<string>() ?? throw new ArgumentException("Missing 'dtype' in Mat JSON.");

            // Преобразуем строку dtype обратно в DepthType
            if (!Enum.TryParse<DepthType>(dtypeStr, out var depthType))
            {
                throw new ArgumentException($"Invalid or unsupported DepthType in JSON: {dtypeStr}");
            }

            // Создаём Mat с правильным размером и типом
            var mat = new Mat(height, width, depthType, channels);

            if (channels == 1)
            {
                if (depthType == DepthType.Cv32F)
                {
                    var flatData = new float[height * width];
                    int idx = 0;
                    for (int r = 0; r < height; r++)
                    {
                        var rowArray = dataJArray[r] as JArray ?? throw new ArgumentException("Invalid row data in Mat JSON.");
                        for (int c = 0; c < width; c++)
                        {
                            flatData[idx++] = rowArray[c].Value<float>();
                        }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(flatData, 0, mat.DataPointer, flatData.Length);
                }
                else if (depthType == DepthType.Cv64F)
                {
                    var flatData = new double[height * width];
                    int idx = 0;
                    for (int r = 0; r < height; r++)
                    {
                        var rowArray = dataJArray[r] as JArray ?? throw new ArgumentException("Invalid row data in Mat JSON.");
                        for (int c = 0; c < width; c++)
                        {
                            flatData[idx++] = rowArray[c].Value<double>();
                        }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(flatData, 0, mat.DataPointer, flatData.Length);
                }
                else
                {
                    throw new ArgumentException($"Unsupported DepthType for single channel Mat: {depthType}");
                }
            }
            else
            {
                throw new ArgumentException($"Multi-channel Mats (channels={channels}) not implemented in MatFromJObject.");
            }

            return mat;
        }

        private static Size GetRequiredSize(JObject jsonData, string key)
        {
            var array = jsonData[key] as JArray
                        ?? throw new ArgumentException($"Missing required JSON array: {key}");

            if (array.Count != 2)
                throw new ArgumentException($"Array '{key}' must contain exactly 2 elements (width, height).");

            return new Size(array[0].Value<int>(), array[1].Value<int>());
        }

        private static SizeF GetRequiredSizeF(JObject jsonData, string key)
        {
            var array = jsonData[key] as JArray
                        ?? throw new ArgumentException($"Missing required JSON array: {key}");

            if (array.Count != 2)
                throw new ArgumentException($"Array '{key}' must contain exactly 2 elements (width, height).");

            return new SizeF(array[0].Value<float>(), array[1].Value<float>());
        }

        private static bool ValidateCameraMatrix(Mat mat)
        {
            // Матрица камеры 3x3
            return mat != null && mat.Size.Equals(new Size(3, 3)) && mat.NumberOfChannels == 1;
        }

        private static bool ValidateDistCoeffs(Mat mat)
        {
            // Коэффициенты искажения, обычно 1xN или Nx1, например, 1x4, 1x5, 1x8
            return mat != null && mat.NumberOfChannels == 1 && (mat.Size.Width == 1 || mat.Size.Height == 1);
        }

        private static bool ValidateMap(Mat mat)
        {
            return mat != null && mat.NumberOfChannels == 1 && mat.Size.Width > 0 && mat.Size.Height > 0;
        }

        private static bool ValidateTForm(Mat mat)
        {
            // Матрица трансформации 3x3
            return mat != null && mat.Size.Equals(new Size(3, 3));
        }
    }
}
