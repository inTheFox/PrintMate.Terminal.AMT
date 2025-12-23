# Hans4Java в C# - Финальная обертка

## 📊 Что мы узнали из декомпиляции

### Классы операций (все простые контейнеры):

```java
// SWEnableOperation.java
public class SWEnableOperation {
    private boolean enable;
    public SWEnableOperation(Boolean enable) {
        this.enable = enable;
        this.setType(OpType.SW_ENABLE);
    }
    public Object[] getData() {
        return new Object[]{this.enable};
    }
}

// DiameterOperation.java
public class DiameterOperation {
    private double diameterMicron;  // В МИКРОНАХ!
    public DiameterOperation(Double diameterMicron) {
        this.diameterMicron = diameterMicron;
        this.setType(OpType.DIAMETER);
    }
}

// PowerOperation.java
public class PowerOperation {
    private float powerW;  // В ВАТТАХ!
    public PowerOperation(Double powerW) {
        this.powerW = powerW.floatValue();
        this.setType(OpType.POWER);
    }
}
```

### DelaysSkyWritingConfig - Defaults:

```java
public static DelaysSkyWritingConfig getDefaultParam() {
    defaultParam.uMax = 0.1F;              // ← Это uniformLen!
    defaultParam.laserOffDelay = 120.0F;
    defaultParam.laserOnDelay = 120.0F;
    defaultParam.markDelay = 1000;
    return defaultParam;
}
```

---

## ✅ C# Wrapper - Финальная версия

```csharp
using System;
using Hans.NET;

namespace PrintMateMC.HansWrapper
{
    /// <summary>
    /// Высокоуровневая обертка над Hans UDM API
    /// Эмулирует поведение Hans4Java
    /// </summary>
    public class HansOperations
    {
        // Defaults из DelaysSkyWritingConfig
        private const float DEFAULT_UMAX = 0.1f;
        private const float DEFAULT_ACC_LEN = 0.05f;  // = uMax / 2
        private const float DEFAULT_ANGLE_LIMIT = 120.0f;

        /// <summary>
        /// Применить операцию SWEnableOperation
        /// Аналог того что делает Hans4Java
        /// </summary>
        public static void ApplySWEnableOperation(bool enable)
        {
            ApplySWEnableOperation(enable, DEFAULT_UMAX, DEFAULT_ACC_LEN, DEFAULT_ANGLE_LIMIT);
        }

        /// <summary>
        /// Применить операцию SWEnableOperation с кастомными параметрами
        /// </summary>
        public static void ApplySWEnableOperation(bool enable, float uMax, float accLen, float angleLimit)
        {
            Console.WriteLine($"SWEnableOperation({enable})");
            Console.WriteLine($"  uMax={uMax}, accLen={accLen}, angleLimit={angleLimit}");

            int enableInt = enable ? 1 : 0;
            int mode = 0;

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enableInt,
                mode,
                uMax,        // uniformLen
                accLen,
                angleLimit
            );
        }

        /// <summary>
        /// Применить операцию DiameterOperation
        /// ВАЖНО: diameter в МИКРОНАХ, нужно преобразовать в Z-offset
        /// </summary>
        public static float ApplyDiameterOperation(double diameterMicron)
        {
            Console.WriteLine($"DiameterOperation({diameterMicron} μm)");

            // Преобразование diameter -> Z offset
            // Нужны параметры калибровки из beamConfig
            const double NOMINAL_DIAMETER = 120.0;  // μm
            const double COEFFICIENT = 0.3;         // mm/10μm

            float zOffset = (float)((diameterMicron - NOMINAL_DIAMETER) / 10.0 * COEFFICIENT);
            Console.WriteLine($"  → Z offset: {zOffset} mm");

            return zOffset;
        }

        /// <summary>
        /// Применить операцию PowerOperation
        /// ВАЖНО: power в ВАТТАХ, нужно преобразовать в проценты
        /// </summary>
        public static float ApplyPowerOperation(double powerW, double maxPower = 500.0)
        {
            Console.WriteLine($"PowerOperation({powerW} W)");

            float powerPercent = (float)(powerW / maxPower * 100.0);
            Console.WriteLine($"  → Power: {powerPercent}%");

            return powerPercent;
        }
    }

    /// <summary>
    /// Примеры использования
    /// </summary>
    public class HansWrapperExamples
    {
        /// <summary>
        /// Пример 1: Простое использование с defaults
        /// </summary>
        public static void Example1_SimpleUsage()
        {
            Console.WriteLine("=== Example 1: Простое использование ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Аналог: new SWEnableOperation(true)
            HansOperations.ApplySWEnableOperation(true);

            // Аналог: new DiameterOperation(80.0)
            float zOffset = HansOperations.ApplyDiameterOperation(80.0);

            // Аналог: new PowerOperation(140.0)
            float powerPercent = HansOperations.ApplyPowerOperation(140.0);

            // Настроить параметры слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 550,
                LaserPower = powerPercent,
                JumpSpeed = 5000
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить геометрию с Z offset
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = zOffset },
                new structUdmPos { x = 10, y = 0, z = zOffset }
            };
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("wrapper_example1.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("\n✅ Файл создан\n");
        }

        /// <summary>
        /// Пример 2: С параметрами из конфигурации
        /// </summary>
        public static void Example2_WithConfig()
        {
            Console.WriteLine("=== Example 2: С параметрами из конфигурации ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Параметры из вашей конфигурации
            float uMax = 0.1f;           // из config.umax
            bool swenable = true;        // из config.swenable
            float accLen = uMax * 0.5f;  // Формула
            float angleLimit = 120.0f;   // Default

            // Применить SWEnableOperation с параметрами
            HansOperations.ApplySWEnableOperation(swenable, uMax, accLen, angleLimit);

            Console.WriteLine("\n✅ SkyWriting применен с параметрами из конфигурации\n");
        }

        /// <summary>
        /// Пример 3: Эмуляция PrintMateMC workflow
        /// </summary>
        public static void Example3_PrintMateMCWorkflow()
        {
            Console.WriteLine("=== Example 3: Эмуляция PrintMateMC workflow ===\n");

            // Список операций (как в PrintMateMC)
            Console.WriteLine("Операции для региона 'edges':");
            Console.WriteLine("  1. DiameterOperation(80.0)");
            Console.WriteLine("  2. PowerOperation(140.0)");
            Console.WriteLine("  3. MarkSpeedOperation(550)");
            Console.WriteLine("  4. SWEnableOperation(true)");
            Console.WriteLine("  5. MarkOperation(0, 0)");
            Console.WriteLine("  6. MarkOperation(10, 0)\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Обработать операции
            float zOffset = HansOperations.ApplyDiameterOperation(80.0);
            float powerPercent = HansOperations.ApplyPowerOperation(140.0);
            HansOperations.ApplySWEnableOperation(true);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 550,
                LaserPower = powerPercent,
                JumpSpeed = 5000
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = zOffset },
                new structUdmPos { x = 10, y = 0, z = zOffset }
            };
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("printmatemc_workflow.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("\n✅ Эмуляция PrintMateMC завершена\n");
        }
    }
}
```

---

## 🔑 Ключевые выводы из декомпиляции

### 1. **Операции - это простые контейнеры**

```java
SWEnableOperation(true)      → boolean enable = true
DiameterOperation(80.0)      → double diameterMicron = 80.0
PowerOperation(140.0)        → float powerW = 140.0
```

### 2. **Defaults из DelaysSkyWritingConfig**

```java
uMax = 0.1F              // ← uniformLen
laserOnDelay = 120.0F
laserOffDelay = 120.0F
markDelay = 1000
```

### 3. **accLen и angleLimit НЕ хранятся в операциях**

Эти параметры **вычисляются** или **берутся из конфигурации** где-то в `UdmProducer`.

Вероятная логика:
```java
float uniformLen = config.uMax;          // 0.1
float accLen = uniformLen * 0.5f;        // 0.05 (эвристика)
float angleLimit = 120.0f;               // Хардкод
```

---

## 📈 Сравнение: Hans4Java vs C# Wrapper

| Аспект | Hans4Java (PrintMateMC) | C# Wrapper | Комментарий |
|--------|------------------------|------------|-------------|
| **Создание операции** | `new SWEnableOperation(true)` | `ApplySWEnableOperation(true)` | Аналогично |
| **Параметры** | Скрыты внутри | Явные или defaults | C# более прозрачен |
| **accLen, angleLimit** | Автоматически | Формула или defaults | Нужно вычислять |
| **DiameterOperation** | Хранит μm | Преобразует в Z | C# делает преобразование |
| **PowerOperation** | Хранит W | Преобразует в % | C# делает преобразование |

---

## 🎯 Итоговые рекомендации

### Для вашего C# кода:

```csharp
// Вместо сложного Hans4Java, используйте простую обертку:

// 1. Применить SkyWriting (аналог new SWEnableOperation(true))
HansOperations.ApplySWEnableOperation(
    enable: true,
    uMax: 0.1f,      // Из конфигурации
    accLen: 0.05f,   // = uMax / 2
    angleLimit: 120.0f
);

// 2. Diameter -> Z offset (аналог new DiameterOperation(80.0))
float z = HansOperations.ApplyDiameterOperation(80.0);

// 3. Power -> % (аналог new PowerOperation(140.0))
float power = HansOperations.ApplyPowerOperation(140.0, maxPower: 500.0);
```

---

## ❓ Что еще нужно декомпилировать

Чтобы **точно** узнать как работает Hans4Java, покажите:

1. **`org/iiv/hlssystem/multi/UdmProducer.class`** - там обрабатываются операции
2. **`org/iiv/hans4java/Udm/Udm.class`** - там вызывается UDM API

Там должен быть код вида:
```java
switch (operation.getType()) {
    case SW_ENABLE:
        boolean enable = (boolean) operation.getData()[0];
        UDM_SetSkyWritingMode(
            enable ? 1 : 0,
            0,
            config.uMax,
            config.uMax * 0.5f,
            120.0f
        );
        break;
}
```

Можете декомпилировать эти классы?
