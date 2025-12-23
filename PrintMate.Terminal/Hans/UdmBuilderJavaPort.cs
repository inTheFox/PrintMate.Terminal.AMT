using Hans.NET.libs;
using Hans.NET.Models;
using Newtonsoft.Json;
using PrintMate.Terminal.Parsers.Shared;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using static Hans.NET.libs.HM_UDM_DLL;
using RegionModel = ProjectParserTest.Parsers.Shared.Models.Region;

namespace PrintMate.Terminal.Hans
{
    /// <summary>
    /// Exact C# port of Java RegionSlicer.java from hans-dev/Hans4Java
    /// This implementation includes interpolation and applies beam diameter exactly as in the original Java code
    /// </summary>
    public class UdmBuilderJavaPort
    {
        public const float ScaleX = 0.8446f;
        public const float ScaleY = 0.9615f;

        private const int MAX_POLYLINE_BUFFER = 30000;
        private const int XY2_100_PROTOCOL_INDEX = 1;
        private const int DIMENSIONAL_3D_INDEX = 1;
        private const float FIELD_MAX_SIZE_Z = 4.0f;
        
        // Команды UDM_JUMP и AddPoint2D перемещают ось Z в 4 раза меньше чем AddPolyline3D
        private const int K_FACTOR_AXES_Z = 4;

        private readonly ScanatorConfiguration _config;
        private readonly string _logsPathDirectory;
        private readonly string _binPathsDirectory;

        // Current state (equivalent to Java's cardProfile.beamConfig.curBeamDiameterMicron)
        private double _currentBeamDiameterMicron;
        private double _currentPowerWatts;
        private int _currentLayerIndex;
        private double _lastZCoord;

        public UdmBuilderJavaPort(ScanatorConfiguration config)
        {
            _config = config;
            _currentBeamDiameterMicron = config.BeamConfig.MinBeamDiameterMicron;
            _currentPowerWatts = config.LaserPowerConfig.MaxPower * 0.5;
            _currentLayerIndex = 0;
            _lastZCoord = 0.0;

            _logsPathDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UdmLogs");
            _binPathsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UdmBinFiles");

            if (!Directory.Exists(_logsPathDirectory)) Directory.CreateDirectory(_logsPathDirectory);
            if (!Directory.Exists(_binPathsDirectory)) Directory.CreateDirectory(_binPathsDirectory);
        }

        public string BuildLayer(Layer layer)
        {
            try
            {
                var stringBuilder = new System.Text.StringBuilder();
                string udmFile = Path.Combine(_binPathsDirectory, $"{_config.CardInfo.SeqIndex}____{Guid.NewGuid()}.bin");

                // Initialize UDM file
                if (UDM_NewFile() != 0)
                    throw new InvalidOperationException("Failed to create UDM file");

                UDM_Main();
                //UDM_SkyWriting(1); // Always enable SkyWriting by default
                ApplyScannerConfig(_config.ScannerConfig);
                //UDM_SetGuidLaser(true);

                // Setup mark parameters for each region (layer in Java terminology)
                MarkParameter[] parameters = new MarkParameter[layer.Regions.Count];
                for (int i = 0; i < layer.Regions.Count; i++)
                {
                    var region = layer.Regions[i];
                    parameters[i] = CreateMarkParameter(region);

                    stringBuilder.AppendLine($"Region {i}: {region.GeometryRegion}, Type: {region.Type}");
                    stringBuilder.AppendLine(JsonConvert.SerializeObject(parameters[i], Formatting.Indented));
                }
                UDM_SetLayersPara(parameters, parameters.Length);

                // Process each region
                for (int regionIndex = 0; regionIndex < layer.Regions.Count; regionIndex++)
                {
                    var region = layer.Regions[regionIndex];
                    stringBuilder.AppendLine($"\n\nProcessing Region {regionIndex}: {region.GeometryRegion}, RegionType: {region.Type}");

                    if (region.PolyLines == null || region.PolyLines.Count == 0)
                        continue;

                    // Update current state from region parameters (like DIAMETER and POWER operations in Java)
                    UpdateCurrentState(region, stringBuilder);

                    // Process all polylines in this region
                    ProcessRegionPolylines(region, regionIndex, stringBuilder);

                }

                // Finalize UDM file
                //UDM_SetGuidLaser(false);
                UDM_SetAnalogValue(0, 0);
                UDM_Jump(0, 0, 0);
                UDM_EndMain();

                int saveResult = UDM_SaveToFile(udmFile);
                if (saveResult != 0)
                    throw new InvalidOperationException($"Failed to save UDM file. Error code: {saveResult}");

                File.WriteAllText(
                    Path.Combine(_logsPathDirectory, $"JAVAPORT_{_config.CardInfo.IpAddress}_{Guid.NewGuid()}.txt"),
                    stringBuilder.ToString()
                );

                Console.WriteLine($"✓ UDM file created: {udmFile}");
                return udmFile;
            }
            catch (Exception e)
            {
                Console.WriteLine($"✗ UdmBuilderJavaPort error: {e}");
                return "";
            }
        }

        private MarkParameter CreateMarkParameter(RegionModel region)
        {
            // Get process variables
            var processVariables = _config.ProcessVariablesMap.NonDepends.First();
            if (_config.FunctionSwitcherConfig.EnableDynamicChangeVariables)
            {
                ProcessVariables closest = _config.ProcessVariablesMap.MarkSpeed[0];
                int minDifference = Math.Abs(_config.ProcessVariablesMap.MarkSpeed[0].MarkSpeed - (int)region.Parameters.LaserSpeed);

                foreach (var config in _config.ProcessVariablesMap.MarkSpeed)
                {
                    int difference = Math.Abs(config.MarkSpeed - (int)region.Parameters.LaserSpeed);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closest = config;
                    }
                }
                processVariables = closest;
            }

            // Calculate power
            float powerWatts = (float)region.Parameters.LaserPower;
            if (_config.FunctionSwitcherConfig.EnablePowerCorrection)
            {
                powerWatts = _config.LaserPowerConfig.GetCorrectPowerWatts(powerWatts);
            }
            float powerPercent = _config.LaserPowerConfig.ConvertPower(powerWatts);
            powerPercent = Math.Max(20, Math.Min(100f, powerPercent));

            var param = new MarkParameter
            {
                MarkSpeed = (uint)processVariables.MarkSpeed,
                JumpSpeed = (uint)processVariables.JumpSpeed,
                MarkDelay = (uint)processVariables.MarkDelay,
                JumpDelay = (uint)processVariables.JumpDelay,
                PolygonDelay = (uint)processVariables.PolygonDelay,
                //LaserPower = powerPercent,
                LaserPower = 50,
                AnalogMode = 1,
                MarkCount = 1,
                LaserOnDelay = (float)processVariables.LaserOnDelay,
                LaserOffDelay = (float)processVariables.LaserOffDelay
            };

            //var param = new MarkParameter
            //{
            //    MarkSpeed = 1,
            //    JumpSpeed = 5000,
            //    MarkDelay = 0,
            //    JumpDelay = 0,
            //    PolygonDelay = 0,
            //    //LaserPower = powerPercent,
            //    LaserPower = 20,
            //    AnalogMode = 1,
            //    MarkCount = 1,
            //    LaserOnDelay = 0,
            //    LaserOffDelay = 0
            //};

            return param;
        }

        private void UpdateCurrentState(RegionModel region, System.Text.StringBuilder log)
        {
            // Equivalent to DIAMETER operation in Java (RegionSlicer.java line 242-246)
            double targetDiameter = region.Parameters.LaserBeamDiameter;
            if (targetDiameter > 0 && targetDiameter >= _config.BeamConfig.MinBeamDiameterMicron)
            {
                _currentBeamDiameterMicron = targetDiameter;
            }
            else
            {
                _currentBeamDiameterMicron = _config.BeamConfig.MinBeamDiameterMicron;
            }

            // Equivalent to POWER operation in Java (RegionSlicer.java line 231-240)
            float powerWatts = (float)region.Parameters.LaserPower;
            if (_config.FunctionSwitcherConfig.EnablePowerCorrection)
            {
                powerWatts = _config.LaserPowerConfig.GetCorrectPowerWatts(powerWatts);
            }
            _currentPowerWatts = powerWatts;

            log.AppendLine($"State updated: BeamDiameter={_currentBeamDiameterMicron:F1}μm, Power={_currentPowerWatts:F1}W");
        }

        private void ProcessRegionPolylines(RegionModel region, int regionIndex, System.Text.StringBuilder log)
        {
            foreach (var polyLine in region.PolyLines)
            {
                if (polyLine.Points == null || polyLine.Points.Count == 0)
                    continue;

                // Step 1: Interpolate points (Java: PointCalculator, RegionSlicer.java line 308-315)
                var interpolatedPoints = InterpolatePolyline(polyLine.Points, maxDistance: 0.1);

                // Step 2: Filter out duplicate points (Java: getFilteredSamePoints, line 387-415)
                var filteredPoints = FilterDuplicatePoints(interpolatedPoints);

                if (filteredPoints.Count < 2)
                {
                    log.AppendLine($"Warning: Polyline has less than 2 unique points after filtering, skipping");
                    continue;
                }

                log.AppendLine($"Polyline: {polyLine.Points.Count} original -> {interpolatedPoints.Count} interpolated -> {filteredPoints.Count} filtered points");

                var hansPoints = new List<structUdmPos>();

                for (int i = 0; i < filteredPoints.Count; i++)
                {
                    var point = filteredPoints[i];

                    // Exact port of Java BeamConfig.getCorrectZValue()
                    double correctedZ = GetCorrectZValue(point.X, point.Y, 0.0f);

                    // IMPORTANT: NO multiplication by K_FACTOR_AXES_Z for UDM_AddPolyline3D!
                    // Java comment (line 27-28): "Commands UDM_JUMP and AddPoint2D move Z axis 4 times less than AddPolyline3D"
                    // This means: K_FACTOR_AXES_Z is ONLY for UDM_Jump and UDM_AddPoint2D (line 149, 323, 332)
                    // UDM_AddPolyline3D uses Z directly without scaling (Java: getPartOfPolylineFromPoints, line 348-382)
                    hansPoints.Add(new structUdmPos
                    {
                        x = point.X,  // Убрано масштабирование, чтобы сохранить пропорции круга
                        y = point.Y,  // Убрано масштабирование, чтобы сохранить пропорции круга
                        z = (float)correctedZ,  // NO multiplication - AddPolyline3D uses Z directly!
                        a = 0
                    });

                    // Log only first point to avoid huge logs
                    if (i == 0)
                    {
                        log.AppendLine($"First point: (0, 0) -> Z={GetCorrectZValue(0, 0, 0):F6}");
                        log.AppendLine($"First point: ({point.X:F3}, {point.Y:F3}) -> Z={correctedZ:F6}");
                    }
                }

                // Step 3: Split polyline if too long (Java: splitPolyline, line 432-457)
                var splitPolylines = SplitPolyline(hansPoints);

                foreach (var polylineChunk in splitPolylines)
                {
                    try
                    {

                        UDM_AddPolyline3D(polylineChunk, polylineChunk.Length, regionIndex);
                        log.AppendLine($"UDM_AddPolyline3D: {JsonConvert.SerializeObject(polylineChunk)}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error adding polyline: {e}");
                        Console.WriteLine(JsonConvert.SerializeObject(polylineChunk, Formatting.Indented));
                    }
                }
            }
        }

        /// <summary>
        /// Exact C# port of Java RegionSlicer.getFilteredSamePoints()
        /// From: RegionSlicer.java lines 387-415
        /// Removes duplicate points with distance threshold 0.0001 mm
        /// </summary>
        private List<Point> FilterDuplicatePoints(List<Point> points)
        {
            if (points.Count < 2)
                return new List<Point>(points);

            var result = new List<Point>();
            var lastPoint = points[0];
            result.Add(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                if (GetLength(lastPoint, points[i]) > 0.0001)
                {
                    result.Add(points[i]);
                    lastPoint = points[i];
                }
            }

            // Ensure last point is always included
            if (result.Count < 2)
            {
                result.Add(points[^1]);
                return result;
            }

            if (GetLength(lastPoint, points[^1]) < 0.0001)
            {
                result[^1] = points[^1];
            }
            else
            {
                result.Add(points[^1]);
            }

            return result;
        }

        /// <summary>
        /// Exact C# port of Java RegionSlicer.getLength()
        /// From: RegionSlicer.java lines 417-422
        /// Note: Point only has X,Y (2D), so we calculate 2D distance
        /// </summary>
        private static double GetLength(Point point1, Point point2)
        {
            return Math.Sqrt(
                Math.Pow(point1.X - point2.X, 2) +
                Math.Pow(point1.Y - point2.Y, 2)
            );
        }

        /// <summary>
        /// Simple linear interpolation between two 2D points
        /// Adds intermediate points based on distance threshold
        /// Java equivalent: PointCalculator (RegionSlicer.java line 308-315)
        /// </summary>
        private static List<Point> InterpolatePoints(Point start, Point end, double maxDistance = 0.1)
        {
            var result = new List<Point>();

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= maxDistance)
            {
                result.Add(start);
                result.Add(end);
                return result;
            }

            int numSegments = (int)Math.Ceiling(distance / maxDistance);

            for (int i = 0; i <= numSegments; i++)
            {
                double t = (double)i / numSegments;
                result.Add(new Point
                {
                    X = (float)(start.X + dx * t),
                    Y = (float)(start.Y + dy * t)
                });
            }

            return result;
        }

        /// <summary>
        /// Interpolate all segments in a polyline (2D)
        /// Java equivalent: getPartOfPolylineFromPoints (RegionSlicer.java line 341-385)
        /// </summary>
        private static List<Point> InterpolatePolyline(List<Point> points, double maxDistance = 0.1)
        {
            if (points.Count < 2)
                return [.. points];

            var result = new List<Point>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var segmentPoints = InterpolatePoints(points[i], points[i + 1], maxDistance);

                // Add all points except the last one (to avoid duplicates)
                for (int j = 0; j < segmentPoints.Count - 1; j++)
                {
                    result.Add(segmentPoints[j]);
                }
            }

            // Add the final point
            result.Add(points[^1]);

            return result;
        }

        /// <summary>
        /// Exact C# port of Java RegionSlicer.splitPolyline()
        /// From: RegionSlicer.java lines 432-457
        /// Splits polylines longer than MAX_POLYLINE_BUFFER into chunks
        /// </summary>
        private List<structUdmPos[]> SplitPolyline(List<structUdmPos> polylines)
        {
            var result = new List<structUdmPos[]>();
            //structUdmPos[] points = new structUdmPos[polylines.Count];
            //for (int i = 0; i < polylines.Count; i++)
            //{
            //    points[i] = polylines[i];
            //}
            //result.Add(points);


            //foreach (var polyline in polylines)
            //{
            //    result.Add();
            //}

            if (polylines.Count <= MAX_POLYLINE_BUFFER)
            {
                result.Add(polylines.ToArray());
                return result;
            }

            int countParts = polylines.Count / MAX_POLYLINE_BUFFER;

            for (int i = 0; i < countParts; i++)
            {
                int startIndex = i * MAX_POLYLINE_BUFFER;
                int count = MAX_POLYLINE_BUFFER;
                var chunk = polylines.GetRange(startIndex, count).ToArray();
                result.Add(chunk);
            }

            int lastPartSize = polylines.Count % MAX_POLYLINE_BUFFER;

            if (lastPartSize != 0)
            {
                int startIndex = polylines.Count - (lastPartSize + 1);
                int count = lastPartSize + 1;
                var chunk = polylines.GetRange(startIndex, count).ToArray();
                result.Add(chunk);
            }

            return result;
        }

        /// <summary>
        /// Exact C# port of Java BeamConfig.getCorrectZValue()
        /// From: hans-dev/Hans4Java/src/org/iiv/hans4java/controlCardProfiles/BeamConfig.java lines 103-117
        /// </summary>
        private double GetCorrectZValue(float coordXMm, float coordYMm, float coordZMm)
        {
            // Line 104: double focalLengthMicron = getCurrentFocalLengthMm(coordXMm, coordYMm, coordZMm) * 1e3;
            double focalLengthMicron = GetCurrentFocalLengthMm(coordXMm, coordYMm, coordZMm) * 1e3;

            // Line 105-107: if (enableDiameterChange) { focalLengthMicron += getLensTravelMicron(curBeamDiameterMicron, minBeamDiameterMicron); }
            if (_config.FunctionSwitcherConfig.EnableDiameterChange)
            {
                focalLengthMicron += GetLensTravelMicron(_currentBeamDiameterMicron, _config.BeamConfig.MinBeamDiameterMicron);
            }

            // Line 108-110: if (enablePowerOffset) { focalLengthMicron -= getPowerOffset(); }
            if (_config.FunctionSwitcherConfig.EnablePowerOffset)
            {
                focalLengthMicron -= GetPowerOffset();
            }

            // Line 111-116: if (enableZCorrection) { return getCalculationZValue(focalLengthMicron * 1e-3); } else { return coordZMm; }
            if (_config.FunctionSwitcherConfig.EnableZCorrection)
            {
                return GetCalculationZValue(focalLengthMicron * 1e-3);
            }
            else
            {
                return coordZMm;
            }
        }

        /// <summary>
        /// Exact C# port of Java BeamConfig.getCurrentFocalLengthMm()
        /// From: BeamConfig.java lines 119-121
        /// </summary>
        private double GetCurrentFocalLengthMm(float coordXMm, float coordYMm, float coordZMm)
        {
            // Line 120: return Math.sqrt(coordXMm*coordXMm + coordYMm*coordYMm + Math.pow(focalLengthMm + coordZMm, 2));
            return Math.Sqrt(
                coordXMm * coordXMm +
                coordYMm * coordYMm +
                Math.Pow(_config.BeamConfig.FocalLengthMm + coordZMm, 2)
            );
        }

        /// <summary>
        /// Exact C# port of Java BeamConfig.getLensTravelMicron()
        /// From: BeamConfig.java lines 199-202
        /// </summary>
        private double GetLensTravelMicron(double beamDiam, double minDiameter)
        {
            // Line 200: if (beamDiam < minDiameter) return 0;
            if (beamDiam < minDiameter) return 0;

            // Line 201: return rayleighLengthMicron * Math.sqrt((((beamDiam / 2) * (beamDiam / 2)) / ((minDiameter / 2) * (minDiameter / 2))) - 1);
            return _config.BeamConfig.RayleighLengthMicron *
                   Math.Sqrt((((beamDiam / 2) * (beamDiam / 2)) / ((minDiameter / 2) * (minDiameter / 2))) - 1);
        }

        /// <summary>
        /// Exact C# port of Java BeamConfig.getPowerOffset()
        /// From: BeamConfig.java lines 188-197
        /// </summary>
        private double GetPowerOffset()
        {
            // Line 195: if (curPower <= maxPower * 0.15) return 0;
            if (_currentPowerWatts <= _config.LaserPowerConfig.MaxPower * 0.15)
                return 0;

            // Line 196: return powerOffsetFunc.value(curPower);
            // In C# we use the BeamConfig method
            return _config.BeamConfig.GetPowerOffset((float)_currentPowerWatts, _config.LaserPowerConfig.MaxPower);
        }

        /// <summary>
        /// Exact C# port of Java BeamConfig.getCalculationZValue()
        /// From: BeamConfig.java lines 205-209
        /// </summary>
        private double GetCalculationZValue(double realFocalLengthMm)
        {
            // Line 206-208: return aFactor * Math.pow(realFocalLengthMm, 2) + bFactor * realFocalLengthMm + cFactor;
            return _config.ThirdAxisConfig.Afactor * Math.Pow(realFocalLengthMm, 2) +
                   _config.ThirdAxisConfig.Bfactor * realFocalLengthMm +
                   _config.ThirdAxisConfig.Cfactor;
        }

        private void ApplyScannerConfig(ScannerConfig config)
        {
            // From Java RegionSlicer.java line 289
            UDM_SetProtocol(XY2_100_PROTOCOL_INDEX, DIMENSIONAL_3D_INDEX);

            if (config.OffsetX != 0 || config.OffsetY != 0 || config.OffsetZ != 0)
            {
                // Применяем масштабирование к offset, чтобы сохранить позицию после коррекции геометрии
                //float scaledOffsetX = config.OffsetX * ScaleX;
                //float scaledOffsetY = config.OffsetY * ScaleY;

                UDM_SetOffset(
                    offsetX: config.OffsetX,
                    offsetY: config.OffsetY,
                    offsetZ: config.OffsetZ
                );
                //Console.WriteLine($"\n\n\nSet offset for: {_config.CardInfo.IpAddress}. X: {scaledOffsetX} (was {config.OffsetX}), Y: {scaledOffsetY} (was {config.OffsetY}), Z: {config.OffsetZ}\n\n\n");
            }

            if (config.RotateAngle != 0)
            {
                UDM_SetRotate(config.RotateAngle, 0, 0);
            }
        }
    }
}
