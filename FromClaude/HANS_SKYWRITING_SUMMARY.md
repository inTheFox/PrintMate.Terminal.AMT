# Hans SkyWriting - Краткая Сводка

## 🎯 Главное открытие

После декомпиляции Hans4Java (`UdmProducer.class`) выяснилось:

### ❗ Hans4Java использует простой API:
```csharp
HM_UDM_DLL.UDM_SkyWriting(enable ? 1 : 0);  // ✅ Так правильно
```

### ❌ НЕ использует расширенный API:
```csharp
UDM_SetSkyWritingMode(enable, mode, uniformLen, accLen, angleLimit);  // ❌ Это не используется
```

### 🔑 Ключевая логика при включении SkyWriting:

```csharp
if (enable)
{
    layers[0].JumpDelay = 0;        // ← КРИТИЧНО: обнулить!
    layers[0].PolygonDelay = 0;     // ← КРИТИЧНО: обнулить!
    layers[0].MarkDelay = markDelayForSkyWriting;
    layers[0].LaserOnDelay = laserOnDelayForSkyWriting;
    layers[0].LaserOffDelay = laserOffDelayForSkyWriting;
}
else
{
    layers[0].JumpDelay = jumpDelayNormal;
    layers[0].PolygonDelay = polygonDelayNormal;
    layers[0].MarkDelay = markDelayNormal;
    layers[0].LaserOnDelay = laserOnDelayNormal;
    layers[0].LaserOffDelay = laserOffDelayNormal;
}

HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
```

---

## 📁 Какие файлы использовать

### ⭐ Начните отсюда:

1. **[HANS_SKYWRITING_COMPLETE_GUIDE.md](HANS_SKYWRITING_COMPLETE_GUIDE.md)**
   - Полное руководство со всем необходимым

2. **[Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs)**
   - Готовый конвертер CLI → Hans
   - Класс `CliToHansConverter`
   - Примеры использования

3. **[Hans_CSharp_Final_Solution.cs](Hans_CSharp_Final_Solution.cs)**
   - Метод `ApplySWEnableOperation_Hans4JavaWay()`
   - Точная копия поведения Hans4Java

### 📖 Дополнительная информация:

4. **[HansSkyWriting_ConfigAnalysis.md](HansSkyWriting_ConfigAnalysis.md)**
   - Анализ вашей конфигурации
   - Откуда берутся параметры

5. **[HansSkyWriting_JavaUsage_Analysis.md](HansSkyWriting_JavaUsage_Analysis.md)**
   - Как Java код использует SkyWriting

---

## 🚀 Быстрый старт (5 минут)

### Вариант 1: Одиночный слой

```csharp
using PrintMateMC.HansFinal;

HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

// Применить SkyWriting точно как Hans4Java
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true,
    laserOnDelayForSkyWriting: 600.0f,
    laserOffDelayForSkyWriting: 730.0f,
    markDelayForSkyWriting: 470,
    laserOnDelayNormal: 420.0f,
    laserOffDelayNormal: 490.0f,
    markDelayNormal: 470,
    jumpDelayNormal: 40000,
    polygonDelayNormal: 385
);

// Добавить геометрию
structUdmPos[] points = new structUdmPos[]
{
    new structUdmPos { x = 0, y = 0, z = -1.2f },
    new structUdmPos { x = 10, y = 0, z = -1.2f },
    new structUdmPos { x = 10, y = 10, z = -1.2f },
    new structUdmPos { x = 0, y = 10, z = -1.2f }
};
HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

### Вариант 2: Конвертация CLI файла

```csharp
using PrintMateMC.HansFinal;

// 1. Создать конфигурацию из вашего JSON
LaserConfig config = new LaserConfig
{
    IpAddress = "172.18.34.227",
    SeqIndex = 0,
    SpeedConfigs = new List<SpeedConfig>
    {
        new SpeedConfig
        {
            MarkSpeed = 800,
            SWEnable = true,
            Umax = 0.1,
            LaserOnDelayForSkyWriting = 600.0,
            LaserOffDelayForSkyWriting = 730.0,
            MarkDelayForSkyWriting = 470,
            LaserOnDelay = 420.0,
            LaserOffDelay = 490.0,
            MarkDelay = 470,
            JumpDelay = 40000,
            PolygonDelay = 385,
            JumpSpeed = 25000,
            CurPower = 140.0,
            CurBeamDiameterMicron = 80.0
        }
    }
};

// 2. Создать регионы из CLI
List<CliRegion> regions = new List<CliRegion>
{
    new CliRegion
    {
        Name = "edges",
        SkyWritingEnabled = true,  // edge_skywriting = "1"
        MarkSpeed = 800,
        LaserPower = 140.0,
        BeamDiameter = 80.0,
        Geometry = new List<CliPoint>
        {
            new CliPoint { X = 0, Y = 0 },
            new CliPoint { X = 10, Y = 0 },
            new CliPoint { X = 10, Y = 10 },
            new CliPoint { X = 0, Y = 10 }
        }
    }
};

// 3. Конвертировать
CliToHansConverter converter = new CliToHansConverter(config);
converter.ConvertFullCliFile(regions, ".");
```

**Результат:**
- ✅ `regions_with_skywriting.bin`
- ✅ `regions_without_skywriting.bin`

---

## 🔍 Ключевые различия с предыдущими подходами

| Аспект | ❌ Старый подход | ✅ Новый подход (Hans4Java) |
|--------|-----------------|---------------------------|
| API вызов | `UDM_SetSkyWritingMode(5 параметров)` | `UDM_SkyWriting(1 параметр)` |
| `JumpDelay` при SW ON | Не обнулялся | **0** |
| `PolygonDelay` при SW ON | Не обнулялся | **0** |
| Задержки | Одинаковые для ON/OFF | Два набора: `*ForSkyWriting` и обычные |
| Источник | Догадки | Декомпилированный Hans4Java |

---

## 📊 Параметры из вашей конфигурации

### Лазер 1 (IP: 172.18.34.227), Скорость 800 mm/s:

```json
{
  "swenable": true,
  "umax": 0.1,
  "laserOnDelayForSkyWriting": 600.0,
  "laserOffDelayForSkyWriting": 730.0,
  "markDelayForSkyWriting": 470,
  "laserOnDelay": 420.0,
  "laserOffDelay": 490.0,
  "markDelay": 470,
  "jumpDelay": 40000,
  "polygonDelay": 385
}
```

### Как использовать эти параметры:

```csharp
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true,                           // swenable
    laserOnDelayForSkyWriting: 600.0f,     // из config
    laserOffDelayForSkyWriting: 730.0f,    // из config
    markDelayForSkyWriting: 470,           // из config
    laserOnDelayNormal: 420.0f,            // из config
    laserOffDelayNormal: 490.0f,           // из config
    markDelayNormal: 470,                  // из config
    jumpDelayNormal: 40000,                // из config
    polygonDelayNormal: 385                // из config
);
```

---

## ❓ FAQ

### Q: Почему `JumpDelay` и `PolygonDelay` обнуляются?

**A:** При SkyWriting лазер остается включенным во время прыжков. Задержки прыжка предназначены для обычного режима (лазер выключается), поэтому при SkyWriting они должны быть 0.

### Q: Где используется параметр `umax`?

**A:** В декомпилированном коде `umax` НЕ передается в `UDM_SkyWriting()`. Возможно:
- Устанавливается через другой API
- Конфигурируется в `system.ini`
- Имеет значение по умолчанию в native DLL

### Q: Можно ли менять SkyWriting в одном файле?

**A:** Нет. UDM API не поддерживает это. Нужно создавать отдельные `.bin` файлы для регионов с разным SkyWriting.

### Q: Какие файлы НЕ использовать?

**A:** Не используйте:
- ❌ `HansSkyWritingExample1-5_*.cs` (старый подход)
- ❌ `HansSkyWritingMode_CliExamples.cs` (не используется в Hans4Java)

---

## 📝 Чеклист для реализации

- [ ] Прочитать [HANS_SKYWRITING_COMPLETE_GUIDE.md](HANS_SKYWRITING_COMPLETE_GUIDE.md)
- [ ] Скопировать классы из [Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs)
- [ ] Извлечь параметры из вашего `scanner_config.json`
- [ ] Создать `LaserConfig` с параметрами для нужных скоростей
- [ ] Парсить CLI файл в `List<CliRegion>`
- [ ] Использовать `CliToHansConverter.ConvertFullCliFile()`
- [ ] Проверить, что создаются отдельные файлы для разных SkyWriting
- [ ] Протестировать на реальном оборудовании

---

## 🎓 Выводы из декомпиляции Hans4Java

1. **Простой API**: `UDM_SkyWriting(boolean)` вместо `UDM_SetSkyWritingMode`
2. **Обнуление задержек**: `JumpDelay = 0`, `PolygonDelay = 0` при SkyWriting ON
3. **Два набора задержек**: Специальные для SkyWriting, обычные для нормального режима
4. **Параметр `umax`**: Не передается в UDM API напрямую
5. **Логика в `updateMarkParam()`**: Ключевой метод в `UdmProducer.class`

---

## 🔗 Структура файлов решения

```
PrinMateMC/
├── HANS_SKYWRITING_SUMMARY.md                    ← 📍 ВЫ ЗДЕСЬ
├── HANS_SKYWRITING_COMPLETE_GUIDE.md             ← Полное руководство
├── Hans_CSharp_Complete_Integration.cs           ← Готовый конвертер
├── Hans_CSharp_Final_Solution.cs                 ← Финальное решение
├── HansSkyWriting_ConfigAnalysis.md              ← Анализ конфигурации
├── HansSkyWriting_JavaUsage_Analysis.md          ← Анализ Java кода
├── Hans_CSharp_HighLevel_API.cs                  ← Высокоуровневая обертка
├── HansSkyWritingMode_README.md                  ← Справка по параметрам
└── [Устаревшие файлы]                            ← Не использовать
    ├── HansSkyWritingExample1_Basic.cs
    ├── HansSkyWritingExample2_Advanced.cs
    ├── HansSkyWritingExample3_FullCliConversion.cs
    ├── HansSkyWritingExample4_PerRegionSwitch.cs
    ├── HansSkyWritingExample5_RealWorldUsage.cs
    └── HansSkyWritingMode_CliExamples.cs
```

---

## 🎯 Следующие шаги

1. **Прочитайте**: [HANS_SKYWRITING_COMPLETE_GUIDE.md](HANS_SKYWRITING_COMPLETE_GUIDE.md)
2. **Используйте**: [Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs)
3. **Тестируйте**: На реальном оборудовании
4. **Калибруйте**: Z-offset для вашей оптической системы

---

## ✅ Готово к использованию

Все файлы готовы к интеграции в ваш проект. Финальное решение основано на **декомпилированном коде** Hans4Java и представляет собой **точную копию** поведения оригинальной Java реализации.

**Удачи!** 🚀

---

**Источник:** Декомпилированный `org.iiv.hlssystem.multi.UdmProducer.class` из Hans4Java
**Версия:** 1.0 (Final)
**Дата:** 2025
