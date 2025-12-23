using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Практическое руководство: Как отправить диаметр пучка в Hans сканер
///
/// Полный цикл обработки от CLI файла до отправки в сканер
/// </summary>
public class HowToSendDiameterToHans
{
    /// <summary>
    /// ШАГ 1: Парсинг диаметра из CLI файла
    /// </summary>
    public class Step1_ParseFromCli
    {
        public static void Example()
        {
            Console.WriteLine("=== ШАГ 1: Парсинг диаметра из CLI файла ===\n");

            // В CLI файле в заголовке есть секция $PARAMETER_SET с JSON
            string cliParameterSetJson = @"{
                ""downskin_hatch_laser_beam_diameter"": 80.0,
                ""downskin_hatch_laser_power"": 280.0,
                ""downskin_hatch_laser_speed"": 800,
                ""upskin_contour_laser_beam_diameter"": 70.0,
                ""upskin_contour_laser_power"": 250.0,
                ""upskin_contour_laser_speed"": 600
            }";

            // Парсим JSON (используйте System.Text.Json или Newtonsoft.Json)
            // Для примера используем словарь
            var parameters = new Dictionary<string, object>
            {
                ["downskin_hatch_laser_beam_diameter"] = 80.0,
                ["downskin_hatch_laser_power"] = 280.0,
                ["downskin_hatch_laser_speed"] = 800,
                ["upskin_contour_laser_beam_diameter"] = 70.0,
                ["upskin_contour_laser_power"] = 250.0,
                ["upskin_contour_laser_speed"] = 600
            };

            // Извлекаем диаметр для региона DOWNSKIN_HATCH
            var diameter = (double)parameters["downskin_hatch_laser_beam_diameter"];

            Console.WriteLine($"✓ Из CLI файла извлечен диаметр: {diameter} μm");
            Console.WriteLine($"  Регион: DOWNSKIN_HATCH");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// ШАГ 2: Создание операций для сканера
    /// </summary>
    public class Step2_CreateOperations
    {
        public static List<IOperation> Example(double diameter, double power, int speed)
        {
            Console.WriteLine("=== ШАГ 2: Создание операций для сканера ===\n");

            var operations = new List<IOperation>();

            // Создаем операцию установки диаметра
            var diameterOp = new DiameterOperation(diameter);
            operations.Add(diameterOp);
            Console.WriteLine($"✓ Создана операция: DiameterOperation({diameter})");

            // Также добавляем связанные параметры
            operations.Add(new PowerOperation(power));
            Console.WriteLine($"✓ Создана операция: PowerOperation({power})");

            operations.Add(new MarkSpeedOperation(speed));
            Console.WriteLine($"✓ Создана операция: MarkSpeedOperation({speed})");

            Console.WriteLine($"\nВсего создано операций: {operations.Count}\n");

            return operations;
        }
    }

    /// <summary>
    /// ШАГ 3: Добавление геометрии к операциям
    /// </summary>
    public class Step3_AddGeometry
    {
        public static void Example(List<IOperation> operations)
        {
            Console.WriteLine("=== ШАГ 3: Добавление геометрии ===\n");

            // Теперь добавляем геометрию, которая будет использовать установленный диаметр
            Console.WriteLine("Добавляем полилинию (контур квадрата):");

            operations.Add(new JumpOperation(-10, -10));
            Console.WriteLine("  JumpOperation(-10, -10) - переход");

            operations.Add(new MarkOperation(10, -10));
            Console.WriteLine("  MarkOperation(10, -10) - рисование");

            operations.Add(new MarkOperation(10, 10));
            Console.WriteLine("  MarkOperation(10, 10) - рисование");

            operations.Add(new MarkOperation(-10, 10));
            Console.WriteLine("  MarkOperation(-10, 10) - рисование");

            operations.Add(new MarkOperation(-10, -10));
            Console.WriteLine("  MarkOperation(-10, -10) - замыкание");

            Console.WriteLine($"\n✓ Добавлено 5 операций геометрии");
            Console.WriteLine($"  Все они будут выполнены с диаметром 80 μm\n");
        }
    }

    /// <summary>
    /// ШАГ 4: Отправка в Hans сканер (через IHLSSystem интерфейс)
    /// </summary>
    public class Step4_SendToScanner
    {
        public static void Example(List<IOperation> operations)
        {
            Console.WriteLine("=== ШАГ 4: Отправка в Hans сканер ===\n");

            // В реальном коде:
            // 1. Получаем экземпляр сканера
            // IHLSSystem scanner = MultiLaserSS.getInstance();

            // 2. Устанавливаем конфигурацию
            // scanner.setConfigurationPath("path/to/scanner_config.json");

            // 3. Реализуем IOperationsProducer интерфейс
            // public class MyOperationsProducer : IOperationsProducer
            // {
            //     private List<IOperation> operations;
            //
            //     public object getOperations()
            //     {
            //         return operations.ToArray();
            //     }
            // }

            // 4. Загружаем операции в сканер
            // scanner.loadOperations(operationsProducer);

            // Для демонстрации:
            Console.WriteLine("Псевдокод отправки в сканер:");
            Console.WriteLine("┌─────────────────────────────────────────────┐");
            Console.WriteLine("│ IHLSSystem scanner = MultiLaserSS.getInstance();");
            Console.WriteLine("│ scanner.setConfigurationPath(configPath);");
            Console.WriteLine("│ scanner.loadOperations(this); // this = IOperationsProducer");
            Console.WriteLine("│ scanner.startProcessing();");
            Console.WriteLine("└─────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("Операции, отправленные в сканер:");
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                string opName = op.GetType().Name;

                if (op is DiameterOperation diam)
                    Console.WriteLine($"  [{i}] {opName} → {diam.Value} μm");
                else if (op is PowerOperation pow)
                    Console.WriteLine($"  [{i}] {opName} → {pow.Value} W");
                else if (op is MarkSpeedOperation spd)
                    Console.WriteLine($"  [{i}] {opName} → {spd.Value} mm/s");
                else if (op is JumpOperation jmp)
                    Console.WriteLine($"  [{i}] {opName} → ({jmp.X:F1}, {jmp.Y:F1})");
                else if (op is MarkOperation mrk)
                    Console.WriteLine($"  [{i}] {opName} → ({mrk.X:F1}, {mrk.Y:F1})");
                else
                    Console.WriteLine($"  [{i}] {opName}");
            }

            Console.WriteLine("\n✓ Операции успешно отправлены в сканер\n");
        }
    }

    /// <summary>
    /// ПОЛНЫЙ ПРИМЕР: От CLI файла до отправки в Hans
    /// </summary>
    public static void FullExample()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ПОЛНЫЙ ПРИМЕР: От CLI файла до Hans сканера        ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ШАГ 1: Парсинг из CLI
        Step1_ParseFromCli.Example();

        // ШАГ 2: Создание операций
        var operations = Step2_CreateOperations.Example(
            diameter: 80.0,
            power: 280.0,
            speed: 800
        );

        // ШАГ 3: Добавление геометрии
        Step3_AddGeometry.Example(operations);

        // ШАГ 4: Отправка в сканер
        Step4_SendToScanner.Example(operations);

        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("✓ Процесс завершен!");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");
    }

    /// <summary>
    /// РЕАЛЬНЫЙ ПРИМЕР: Класс-обертка для работы с Hans (как в PrintMateMC)
    /// </summary>
    public class RealWorldExample
    {
        // Интерфейс IHLSSystem (упрощенная версия из org.iiv.hlssystem)
        public interface IHLSSystem
        {
            int SetConfigurationPath(string path);
            int LoadOperations(IOperationsProducer producer);
            int StartProcessing();
            void Close();
        }

        // Интерфейс IOperationsProducer (из org.iiv.hlssystem.Operations)
        public interface IOperationsProducer
        {
            object GetOperations();  // Возвращает IOperation[] или List<IOperation>
        }

        // Класс-менеджер операций (аналог CommandManager из PrintMateMC)
        public class ScanOperationsManager : IOperationsProducer
        {
            private List<IOperation> currentLayerOperations;
            private IHLSSystem scanner;

            public ScanOperationsManager(IHLSSystem scanner)
            {
                this.scanner = scanner;
                this.currentLayerOperations = new List<IOperation>();
            }

            // Метод для загрузки слоя из CLI
            public void LoadLayerFromCli(int layerNumber, string cliFilePath)
            {
                Console.WriteLine($"Загрузка слоя {layerNumber} из {cliFilePath}...");

                currentLayerOperations.Clear();

                // 1. Парсим CLI файл и получаем параметры
                var parameters = ParseCliParameters(cliFilePath);

                // 2. Парсим геометрию слоя
                var geometries = ParseCliGeometry(cliFilePath, layerNumber);

                // 3. Для каждого типа геометрии создаем операции
                foreach (var geom in geometries)
                {
                    // Получаем параметры для региона
                    var diameter = GetDiameterForRegion(parameters, geom.Region);
                    var power = GetPowerForRegion(parameters, geom.Region);
                    var speed = GetSpeedForRegion(parameters, geom.Region);

                    // Добавляем операции настройки
                    currentLayerOperations.Add(new DiameterOperation(diameter));
                    currentLayerOperations.Add(new PowerOperation(power));
                    currentLayerOperations.Add(new MarkSpeedOperation(speed));

                    // Добавляем операции геометрии
                    currentLayerOperations.AddRange(geom.Operations);

                    Console.WriteLine($"  Регион {geom.Region}: диаметр {diameter} μm, " +
                                    $"{geom.Operations.Count} операций");
                }

                Console.WriteLine($"✓ Загружено операций: {currentLayerOperations.Count}\n");
            }

            // Метод для отправки операций в сканер
            public void SendToScanner()
            {
                Console.WriteLine("Отправка операций в Hans сканер...");

                // Hans сканер вызовет GetOperations() для получения операций
                int result = scanner.LoadOperations(this);

                if (result == 0)  // SSystem_NO_ERROR
                {
                    Console.WriteLine("✓ Операции успешно загружены в сканер");

                    result = scanner.StartProcessing();
                    if (result == 0)
                        Console.WriteLine("✓ Сканер начал обработку\n");
                    else
                        Console.WriteLine($"✗ Ошибка запуска: код {result}\n");
                }
                else
                {
                    Console.WriteLine($"✗ Ошибка загрузки операций: код {result}\n");
                }
            }

            // Реализация интерфейса IOperationsProducer
            public object GetOperations()
            {
                // Hans сканер вызывает этот метод для получения операций
                Console.WriteLine($"[IOperationsProducer] GetOperations() вызван, " +
                                $"возвращаем {currentLayerOperations.Count} операций");

                // Возвращаем массив операций
                return currentLayerOperations.ToArray();
            }

            // Вспомогательные методы (заглушки)
            private Dictionary<string, object> ParseCliParameters(string path)
            {
                return new Dictionary<string, object>
                {
                    ["downskin_hatch_laser_beam_diameter"] = 80.0,
                    ["downskin_hatch_laser_power"] = 280.0,
                    ["downskin_hatch_laser_speed"] = 800,
                    ["infill_hatch_laser_beam_diameter"] = 90.0,
                    ["infill_hatch_laser_power"] = 350.0,
                    ["infill_hatch_laser_speed"] = 1400
                };
            }

            private List<GeometryRegionData> ParseCliGeometry(string path, int layer)
            {
                return new List<GeometryRegionData>
                {
                    new GeometryRegionData
                    {
                        Region = "DOWNSKIN",
                        Operations = new List<IOperation>
                        {
                            new JumpOperation(-10, -10),
                            new MarkOperation(10, -10),
                            new MarkOperation(10, 10)
                        }
                    },
                    new GeometryRegionData
                    {
                        Region = "INFILL",
                        Operations = new List<IOperation>
                        {
                            new JumpOperation(-8, -8),
                            new MarkOperation(8, -8)
                        }
                    }
                };
            }

            private double GetDiameterForRegion(Dictionary<string, object> p, string region)
            {
                string key = $"{region.ToLower()}_hatch_laser_beam_diameter";
                return p.ContainsKey(key) ? (double)p[key] : 80.0;
            }

            private double GetPowerForRegion(Dictionary<string, object> p, string region)
            {
                string key = $"{region.ToLower()}_hatch_laser_power";
                return p.ContainsKey(key) ? (double)p[key] : 280.0;
            }

            private int GetSpeedForRegion(Dictionary<string, object> p, string region)
            {
                string key = $"{region.ToLower()}_hatch_laser_speed";
                return p.ContainsKey(key) ? (int)(double)p[key] : 800;
            }
        }

        public class GeometryRegionData
        {
            public string Region { get; set; }
            public List<IOperation> Operations { get; set; }
        }

        // Заглушка для Hans сканера
        public class MockHansScanner : IHLSSystem
        {
            private IOperationsProducer producer;

            public int SetConfigurationPath(string path)
            {
                Console.WriteLine($"[Hans] Установлен путь конфигурации: {path}");
                return 0;
            }

            public int LoadOperations(IOperationsProducer prod)
            {
                this.producer = prod;
                Console.WriteLine("[Hans] IOperationsProducer зарегистрирован");

                // Hans сканер вызывает GetOperations() для получения операций
                var ops = producer.GetOperations();

                if (ops is IOperation[] opsArray)
                {
                    Console.WriteLine($"[Hans] Получено операций: {opsArray.Length}");
                    return 0;
                }

                return -1;
            }

            public int StartProcessing()
            {
                Console.WriteLine("[Hans] Начинаем обработку операций...");

                // В реальности здесь Hans выполняет операции
                var ops = producer.GetOperations() as IOperation[];
                if (ops != null)
                {
                    foreach (var op in ops.Take(5))  // Показываем первые 5
                    {
                        if (op is DiameterOperation d)
                            Console.WriteLine($"[Hans] → Устанавливаем диаметр {d.Value} μm");
                        else if (op is PowerOperation p)
                            Console.WriteLine($"[Hans] → Устанавливаем мощность {p.Value} W");
                        else if (op is MarkSpeedOperation s)
                            Console.WriteLine($"[Hans] → Устанавливаем скорость {s.Value} mm/s");
                        else if (op is JumpOperation j)
                            Console.WriteLine($"[Hans] → Прыжок к ({j.X}, {j.Y})");
                        else if (op is MarkOperation m)
                            Console.WriteLine($"[Hans] → Рисование к ({m.X}, {m.Y})");
                    }

                    if (ops.Length > 5)
                        Console.WriteLine($"[Hans] → ... еще {ops.Length - 5} операций");
                }

                return 0;
            }

            public void Close()
            {
                Console.WriteLine("[Hans] Сканер закрыт");
            }
        }

        // ЗАПУСК РЕАЛЬНОГО ПРИМЕРА
        public static void Run()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║  РЕАЛЬНЫЙ ПРИМЕР: Работа с Hans как в PrintMateMC   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // 1. Создаем экземпляр Hans сканера
            IHLSSystem scanner = new MockHansScanner();

            // 2. Устанавливаем конфигурацию
            scanner.SetConfigurationPath("scanner_config.json");
            Console.WriteLine();

            // 3. Создаем менеджер операций
            var manager = new ScanOperationsManager(scanner);

            // 4. Загружаем слой из CLI файла
            manager.LoadLayerFromCli(42, "job_file.cli");

            // 5. Отправляем в сканер
            manager.SendToScanner();

            // 6. Закрываем сканер
            scanner.Close();

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("✓ Реальный пример завершен!");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");
        }
    }

    // ГЛАВНАЯ ФУНКЦИЯ
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Запускаем полный пример
        FullExample();

        Console.WriteLine("\n" + new string('═', 60) + "\n");

        // Запускаем реальный пример
        RealWorldExample.Run();

        Console.WriteLine("\n\n💡 РЕЗЮМЕ:");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("1. Парсите диаметр из CLI JSON параметров");
        Console.WriteLine("2. Создаете DiameterOperation(80.0)");
        Console.WriteLine("3. Добавляете в список операций ПЕРЕД геометрией");
        Console.WriteLine("4. Реализуете IOperationsProducer интерфейс");
        Console.WriteLine("5. Передаете операции в scanner.LoadOperations(this)");
        Console.WriteLine("6. Hans вызовет GetOperations() и получит ваш список");
        Console.WriteLine("7. Диаметр применится ко всей последующей геометрии!");
        Console.WriteLine("───────────────────────────────────────────────────────────\n");
    }
}

#region Определения классов (те же, что раньше)

public interface IOperation { }

public class DiameterOperation : IOperation
{
    public double Value { get; }
    public DiameterOperation(double value) => Value = value;
}

public class PowerOperation : IOperation
{
    public double Value { get; }
    public PowerOperation(double value) => Value = value;
}

public class MarkSpeedOperation : IOperation
{
    public int Value { get; }
    public MarkSpeedOperation(int value) => Value = value;
}

public class MarkOperation : IOperation
{
    public double X { get; }
    public double Y { get; }
    public MarkOperation(double x, double y) { X = x; Y = y; }
}

public class JumpOperation : IOperation
{
    public double X { get; }
    public double Y { get; }
    public JumpOperation(double x, double y) { X = x; Y = y; }
}

#endregion
