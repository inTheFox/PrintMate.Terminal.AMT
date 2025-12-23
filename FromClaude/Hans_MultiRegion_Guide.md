# Hans UDM: Как формировать слой с множеством регионов

## 🎯 Три варианта организации

### ВАРИАНТ 1: Все регионы в ОДИН слой ✅ (рекомендуется!)

```
Layer 0:
  → edges (SW=ON, speed=800, power=140W, d=80μm)
  → downskin_border (SW=ON, speed=800, power=150W, d=90μm)
  → downskin_hatch (SW=ON, speed=1250, power=180W, d=95μm)
  → infill_border (SW=ON, speed=800, power=160W, d=85μm)
  → infill_hatch (SW=ON, speed=1250, power=220W, d=100μm)
  → upskin_border (SW=ON, speed=800, power=155W, d=88μm)
  → upskin_hatch (SW=ON, speed=1250, power=200W, d=95μm)
  → support_border (SW=OFF, speed=2000, power=280W, d=110μm)
  → support_hatch (SW=OFF, speed=2000, power=320W, d=120μm)
```

**Как:** Все регионы добавляются с `layerIndex = 0`

**Преимущества:**
- ✅ Простой код
- ✅ Hans автоматически переключает параметры
- ✅ Естественная структура файла

---

### ВАРИАНТ 2: Каждый регион в ОТДЕЛЬНОМ слое

```
Layer 0: edges
Layer 1: downskin_border
Layer 2: downskin_hatch
Layer 3: infill_border
...
Layer 8: support_hatch
```

**Как:** Каждый регион добавляется с `layerIndex++`

**Когда использовать:**
- Нужен точный контроль порядка сканирования
- Разные регионы печатаются в разное время

---

### ВАРИАНТ 3: Группировка по параметрам

```
Layer 0: edges + downskin_border + infill_border + upskin_border
         (все с speed=800, SW=ON)

Layer 1: downskin_hatch + infill_hatch + upskin_hatch
         (все с speed=1250, SW=ON)

Layer 2: support_border + support_hatch
         (все с speed=2000, SW=OFF)
```

**Как:** Группировать регионы с одинаковыми (SW, speed, power, diameter)

**Преимущества:**
- ✅ Минимизирует переключения параметров
- ✅ Может быть быстрее

---

## 💻 Код для ВАРИАНТА 1 (рекомендуется)

```csharp
// 1. Инициализация
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

int layerIndex = 0;  // ← ВСЕ регионы идут в layer 0

// 2. Для каждого региона
foreach (var region in layerRegions)
{
    // 2.1. Установить параметры для этого региона
    SetLayerParameters(region);

    // 2.2. Добавить геометрию
    AddRegionGeometry(region, layerIndex);  // layerIndex = 0 для всех!
}

// 3. Генерация и сохранение
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

---

## 🔧 Детали реализации

### SetLayerParameters (вызывается для каждого региона)

```csharp
private void SetLayerParameters(CliRegion region, SpeedConfig speedConfig)
{
    // 1. Установить SkyWriting
    HM_UDM_DLL.UDM_SkyWriting(region.SkyWritingEnabled ? 1 : 0);

    // 2. Установить параметры слоя
    MarkParameter[] layers = new MarkParameter[1];
    layers[0] = new MarkParameter
    {
        MarkSpeed = (uint)region.MarkSpeed,
        JumpSpeed = (uint)speedConfig.JumpSpeed,
        LaserPower = (float)(region.LaserPower / maxPower * 100.0),
        MarkCount = 1
    };

    // 3. Задержки
    if (region.SkyWritingEnabled)
    {
        layers[0].JumpDelay = 0;       // ← НОЛЬ!
        layers[0].PolygonDelay = 0;    // ← НОЛЬ!
        layers[0].LaserOnDelay = (float)speedConfig.LaserOnDelayForSkyWriting;
        layers[0].LaserOffDelay = (float)speedConfig.LaserOffDelayForSkyWriting;
    }
    else
    {
        layers[0].JumpDelay = (uint)speedConfig.JumpDelay;
        layers[0].PolygonDelay = (uint)speedConfig.PolygonDelay;
        layers[0].LaserOnDelay = (float)speedConfig.LaserOnDelay;
        layers[0].LaserOffDelay = (float)speedConfig.LaserOffDelay;
    }

    HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
}
```

### AddRegionGeometry

```csharp
private void AddRegionGeometry(CliRegion region, int layerIndex)
{
    // 1. Рассчитать Z-offset для диаметра
    float z_diameter = beamConfig.CalculateZOffset(region.BeamDiameter);

    // 2. Для каждой полилинии
    foreach (var polyline in region.Polylines)
    {
        structUdmPos[] points = new structUdmPos[polyline.Points.Count];

        // 3. Для каждой точки
        for (int i = 0; i < polyline.Points.Count; i++)
        {
            float x = polyline.Points[i].X;
            float y = polyline.Points[i].Y;

            // Рассчитать итоговый Z
            float z_field = thirdAxisConfig.CalculateFieldCorrection(x, y);
            float z_total = z_diameter + z_field + (float)staticOffsetZ;

            points[i] = new structUdmPos { x = x, y = y, z = z_total };
        }

        // 4. Добавить полилинию в Hans
        HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
    }
}
```

---

## 📊 Структура вызовов API

```
HM_UDM_DLL.UDM_NewFile()
HM_UDM_DLL.UDM_SetProtocol(0, 1)

┌─ РЕГИОН 1 (edges) ─────────────────────────┐
│  HM_UDM_DLL.UDM_SkyWriting(1)              │
│  HM_UDM_DLL.UDM_SetLayersPara(...)         │
│  HM_UDM_DLL.UDM_AddPolyline3D(..., 0)      │ ← layerIndex = 0
└────────────────────────────────────────────┘

┌─ РЕГИОН 2 (downskin_border) ───────────────┐
│  HM_UDM_DLL.UDM_SkyWriting(1)              │
│  HM_UDM_DLL.UDM_SetLayersPara(...)         │
│  HM_UDM_DLL.UDM_AddPolyline3D(..., 0)      │ ← layerIndex = 0
└────────────────────────────────────────────┘

┌─ РЕГИОН 3 (infill_hatch) ──────────────────┐
│  HM_UDM_DLL.UDM_SkyWriting(1)              │
│  HM_UDM_DLL.UDM_SetLayersPara(...)         │
│  HM_UDM_DLL.UDM_AddPolyline3D(..., 0)      │ ← layerIndex = 0
│  HM_UDM_DLL.UDM_AddPolyline3D(..., 0)      │ ← Несколько полилиний
│  HM_UDM_DLL.UDM_AddPolyline3D(..., 0)      │
└────────────────────────────────────────────┘

... (остальные регионы)

HM_UDM_DLL.UDM_Main()
HM_UDM_DLL.UDM_SaveToFile("output.bin")
HM_UDM_DLL.UDM_EndMain()
```

---

## ❓ FAQ

### Q: Нужно ли вызывать `UDM_SetLayersPara` для каждого региона?

**A:** ДА! Каждый регион может иметь разные параметры (speed, power, SkyWriting).

### Q: Что если у двух регионов одинаковые параметры?

**A:** Все равно вызовите `UDM_SetLayersPara`. Hans оптимизирует это автоматически.

### Q: Можно ли менять SkyWriting между регионами?

**A:** ДА! Вызовите `UDM_SkyWriting(0/1)` перед каждым регионом.

### Q: Что будет, если не вызвать `UDM_SetLayersPara`?

**A:** Регион будет использовать параметры предыдущего региона (ПЛОХО!).

### Q: Сколько полилиний можно добавить в один слой?

**A:** Практически неограниченно. Все зависит от памяти.

### Q: Нужно ли группировать polylines одного региона?

**A:** НЕТ. Просто добавляйте все полилинии с одинаковым `layerIndex`.

---

## 🎯 Рекомендации

### ✅ DO (Делайте так):

1. **Используйте ВАРИАНТ 1** (все регионы в один слой)
2. **Вызывайте `UDM_SetLayersPara`** перед каждым регионом
3. **Вызывайте `UDM_SkyWriting`** если меняется SkyWriting
4. **Используйте одинаковый `layerIndex`** для всех регионов слоя

### ❌ DON'T (Не делайте так):

1. ❌ Забывать вызвать `UDM_SetLayersPara` для региона
2. ❌ Использовать разные `layerIndex` без необходимости
3. ❌ Предполагать, что параметры "запомнятся" между регионами

---

## 📈 Порядок сканирования

Hans сканирует регионы **в том порядке**, в котором они добавлены:

```
Порядок добавления:
  1. edges
  2. downskin_border
  3. infill_hatch
  4. support_hatch

Порядок сканирования:
  1. edges         ← Первым
  2. downskin_border
  3. infill_hatch
  4. support_hatch  ← Последним
```

**Контроль порядка:**
- Добавляйте регионы в нужном порядке
- Или используйте ВАРИАНТ 2 (отдельные слои)

---

## 📁 Полный пример

Смотрите: **[Hans_MultiRegion_Layer_Example.cs](Hans_MultiRegion_Layer_Example.cs)**

Содержит:
- ✅ Все 3 варианта организации
- ✅ Реальный слой с 9 регионами
- ✅ Генерация штриховки
- ✅ Полная обработка параметров

---

## 🎓 Итог

```
ТИПИЧНЫЙ СЛОЙ:
  9 регионов
  → edges, downskin_border, downskin_hatch
  → infill_border, infill_hatch
  → upskin_border, upskin_hatch
  → support_border, support_hatch

РЕШЕНИЕ:
  Все регионы → layerIndex = 0
  Перед каждым регионом:
    1. UDM_SkyWriting(...)
    2. UDM_SetLayersPara(...)
    3. UDM_AddPolyline3D(..., 0)  ← layerIndex = 0
```

**Просто и работает!** ✅

---

**Версия:** 1.0
**Дата:** 2025
