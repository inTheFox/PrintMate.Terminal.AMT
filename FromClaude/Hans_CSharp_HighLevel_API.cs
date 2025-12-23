using System;
using System.Collections.Generic;
using Hans.NET;

namespace PrintMateMC.HansHighLevel
{
    /// <summary>
    /// Высокоуровневая C# обертка над Hans UDM API
    /// Эмулирует поведение Hans4Java для упрощения работы
    /// </summary>
    public class HansHighLevelAPI
    {
        /// <summary>
        /// Типы операций (аналог OpType из Hans4Java)
        /// </summary>
        public enum OperationType
        {
            MARK,               // Печать точки
            JUMP,               // Прыжок без печати
            DIAMETER,           // Установка диаметра луча (focus)
            POWER,              // Установка мощности
            MARK_SPEED,         // Установка скорости печати
            JUMP_SPEED,         // Установка скорости прыжка
            SW_ENABLE,          // Включение/выключение SkyWriting
            LASER_ON_DELAY,     // Задержка включения лазера
            LASER_OFF_DELAY,    // Задержка выключения лазера
            MARK_DELAY,         // Задержка маркировки
            JUMP_DELAY,         // Задержка прыжка
            POLYGON_DELAY       // Задержка полигона
        }

        /// <summary>
        /// Базовая операция (аналог IOperation из Hans4Java)
        /// </summary>
        public abstract class Operation
        {
            public OperationType Type { get; protected set; }
            public abstract object[] GetData();
            public abstract bool IsValid();
        }

        /// <summary>
        /// Операция включения/выключения SkyWriting
        /// Аналог org.iiv.hlssystem.Operations.AdditionalOperation.SWEnableOperation
        /// </summary>
        public class SWEnableOperation : Operation
        {
            private bool enable;

            public SWEnableOperation(bool enable)
            {
                this.enable = enable;
                this.Type = OperationType.SW_ENABLE;
            }

            public override object[] GetData()
            {
                return new object[] { enable };
            }

            public override bool IsValid()
            {
                return true;
            }

            public override string ToString()
            {
                return $"SWEnableOperation({enable})";
            }
        }

        /// <summary>
        /// Операция установки диаметра луча (focus)
        /// Аналог DiameterOperation
        /// </summary>
        public class DiameterOperation : Operation
        {
            private double diameter;

            public DiameterOperation(double diameter)
            {
                this.diameter = diameter;
                this.Type = OperationType.DIAMETER;
            }

            public override object[] GetData()
            {
                return new object[] { diameter };
            }

            public override bool IsValid()
            {
                return diameter > 0;
            }

            public override string ToString()
            {
                return $"DiameterOperation({diameter})";
            }
        }

        /// <summary>
        /// Операция установки мощности
        /// Аналог PowerOperation
        /// </summary>
        public class PowerOperation : Operation
        {
            private double power;

            public PowerOperation(double power)
            {
                this.power = power;
                this.Type = OperationType.POWER;
            }

            public override object[] GetData()
            {
                return new object[] { power };
            }

            public override bool IsValid()
            {
                return power >= 0;
            }

            public override string ToString()
            {
                return $"PowerOperation({power})";
            }
        }

        /// <summary>
        /// Операция установки скорости печати
        /// Аналог MarkSpeedOperation
        /// </summary>
        public class MarkSpeedOperation : Operation
        {
            private int speed;

            public MarkSpeedOperation(int speed)
            {
                this.speed = speed;
                this.Type = OperationType.MARK_SPEED;
            }

            public override object[] GetData()
            {
                return new object[] { speed };
            }

            public override bool IsValid()
            {
                return speed > 0;
            }

            public override string ToString()
            {
                return $"MarkSpeedOperation({speed})";
            }
        }

        /// <summary>
        /// Операция печати точки
        /// Аналог MarkOperation
        /// </summary>
        public class MarkOperation : Operation
        {
            private double x, y;

            public MarkOperation(double x, double y)
            {
                this.x = x;
                this.y = y;
                this.Type = OperationType.MARK;
            }

            public override object[] GetData()
            {
                return new object[] { x, y };
            }

            public override bool IsValid()
            {
                return true;
            }

            public override string ToString()
            {
                return $"MarkOperation({x}, {y})";
            }
        }

        /// <summary>
        /// Операция прыжка без печати
        /// Аналог JumpOperation
        /// </summary>
        public class JumpOperation : Operation
        {
            private double x, y;

            public JumpOperation(double x, double y)
            {
                this.x = x;
                this.y = y;
                this.Type = OperationType.JUMP;
            }

            public override object[] GetData()
            {
                return new object[] { x, y };
            }

            public override bool IsValid()
            {
                return true;
            }

            public override string ToString()
            {
                return $"JumpOperation({x}, {y})";
            }
        }

        /// <summary>
        /// Конвертер операций в вызовы Hans UDM API
        /// Аналог UdmProducer из Hans4Java
        /// </summary>
        public class OperationConverter
        {
            private float lastUniformLen = 0.1f;
            private float lastAccLen = 0.05f;
            private float lastAngleLimit = 120.0f;

            /// <summary>
            /// Конвертировать список операций в вызовы UDM API
            /// </summary>
            public void ConvertAndApplyOperations(List<Operation> operations, int layerIndex)
            {
                MarkParameter currentParams = new MarkParameter();
                List<structUdmPos> points = new List<structUdmPos>();
                bool geometryStarted = false;

                foreach (var op in operations)
                {
                    switch (op.Type)
                    {
                        case OperationType.SW_ENABLE:
                            ApplySWEnable((bool)op.GetData()[0]);
                            break;

                        case OperationType.DIAMETER:
                            // DiameterOperation конвертируется в Z-offset
                            double diameter = (double)op.GetData()[0];
                            // Здесь должна быть формула преобразования diameter -> Z
                            break;

                        case OperationType.POWER:
                            currentParams.LaserPower = (float)(double)op.GetData()[0];
                            break;

                        case OperationType.MARK_SPEED:
                            currentParams.MarkSpeed = (uint)(int)op.GetData()[0];
                            break;

                        case OperationType.MARK:
                            if (!geometryStarted)
                            {
                                // Перед геометрией применить параметры
                                ApplyMarkParameters(currentParams, layerIndex);
                                geometryStarted = true;
                            }
                            double x = (double)op.GetData()[0];
                            double y = (double)op.GetData()[1];
                            points.Add(new structUdmPos { x = (float)x, y = (float)y, z = 0 });
                            break;

                        case OperationType.JUMP:
                            // Jump обрабатывается отдельно
                            break;
                    }
                }

                // Добавить геометрию
                if (points.Count > 0)
                {
                    HM_UDM_DLL.UDM_AddPolyline3D(points.ToArray(), points.Count, layerIndex);
                }
            }

            /// <summary>
            /// Применить операцию SWEnableOperation
            /// Это ключевая функция - преобразует высокоуровневую операцию в низкоуровневый вызов
            /// </summary>
            private void ApplySWEnable(bool enable)
            {
                Console.WriteLine($"Applying SWEnableOperation: {enable}");

                // Аналог того что делает Hans4Java внутри:
                int enableInt = enable ? 1 : 0;
                int mode = 0;

                // Используем последние сохраненные значения или defaults
                float uniformLen = lastUniformLen;
                float accLen = lastAccLen;
                float angleLimit = lastAngleLimit;

                Console.WriteLine($"  Calling UDM_SetSkyWritingMode({enableInt}, {mode}, {uniformLen}, {accLen}, {angleLimit})");

                HM_UDM_DLL.UDM_SetSkyWritingMode(
                    enableInt,
                    mode,
                    uniformLen,
                    accLen,
                    angleLimit
                );
            }

            /// <summary>
            /// Установить параметры SkyWriting для последующих операций
            /// </summary>
            public void SetSkyWritingParams(float uniformLen, float accLen, float angleLimit)
            {
                this.lastUniformLen = uniformLen;
                this.lastAccLen = accLen;
                this.lastAngleLimit = angleLimit;
            }

            private void ApplyMarkParameters(MarkParameter param, int layerIndex)
            {
                MarkParameter[] layers = new MarkParameter[] { param };
                HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
            }
        }

        /// <summary>
        /// Пример использования высокоуровневого API
        /// </summary>
        public static void Example_HighLevelAPI()
        {
            Console.WriteLine("=== Example: High-Level API (аналог Hans4Java) ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Создать список операций (аналог того что делает PrintMateMC)
            var operations = new List<Operation>
            {
                // Конфигурационные операции
                new DiameterOperation(80.0),        // Focus: 80 микрон
                new PowerOperation(140.0),          // Power: 140 W
                new MarkSpeedOperation(550),        // Speed: 550 mm/s
                new SWEnableOperation(true),        // ← SkyWriting ON

                // Геометрия
                new JumpOperation(0, 0),
                new MarkOperation(10, 0),
                new MarkOperation(10, 10),
                new MarkOperation(0, 10),
                new MarkOperation(0, 0)
            };

            Console.WriteLine("Операции для обработки:");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op}");
            }
            Console.WriteLine();

            // Конвертировать операции в UDM вызовы
            var converter = new OperationConverter();
            converter.SetSkyWritingParams(0.1f, 0.05f, 120.0f);
            converter.ConvertAndApplyOperations(operations, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("highlevel_api_output.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("✅ Файл создан с использованием высокоуровневого API\n");
        }

        /// <summary>
        /// Пример: Переключение SkyWriting между регионами
        /// (В реальности нужно создавать отдельные файлы)
        /// </summary>
        public static void Example_MultipleRegionsWithSWEnable()
        {
            Console.WriteLine("=== Example: Multiple Regions с SWEnableOperation ===\n");

            // Регион 1: Edges с SkyWriting
            var edgesOps = new List<Operation>
            {
                new DiameterOperation(80.0),
                new PowerOperation(140.0),
                new MarkSpeedOperation(550),
                new SWEnableOperation(true),        // ← ON для edges
                new MarkOperation(0, 0),
                new MarkOperation(5, 0)
            };

            // Регион 2: Supports БЕЗ SkyWriting
            var supportsOps = new List<Operation>
            {
                new DiameterOperation(80.0),
                new PowerOperation(260.0),
                new MarkSpeedOperation(900),
                new SWEnableOperation(false),       // ← OFF для supports
                new MarkOperation(10, 10),
                new MarkOperation(15, 10)
            };

            Console.WriteLine("Edges операции:");
            edgesOps.ForEach(op => Console.WriteLine($"  {op}"));

            Console.WriteLine("\nSupports операции:");
            supportsOps.ForEach(op => Console.WriteLine($"  {op}"));

            Console.WriteLine("\n⚠️ ВАЖНО: В реальности нужно создавать ОТДЕЛЬНЫЕ файлы!");
            Console.WriteLine("   UDM API не поддерживает изменение SkyWriting в одном файле.\n");
        }
    }
}
