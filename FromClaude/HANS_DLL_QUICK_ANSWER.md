# Как применить диаметр пучка в HM_HashuScan.dll

## ⚠️ ВАЖНО: Прямой функции НЕТ!

В `HM_HashuScan.dll` **НЕТ** функции для прямой установки диаметра типа:
```csharp
❌ UDM_SetDiameter(80.0);  // Такой функции не существует!
```

## ✅ РЕШЕНИЕ: Используйте параметр Z в 3D режиме

### Быстрый ответ (копируй-вставляй):

```csharp
// У вас есть диаметр из CLI
double diameter = 80.0; // μm

// ШАГ 1: Включите 3D режим
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1); // ← 1 = 3D режим!

// ШАГ 2: Конвертируйте диаметр в Z-смещение
double nominalDiameter = 70.0; // μm при Z=0 (из конфига сканера)
double zPerDiameter = 0.1;     // мм Z на 10 μm изменения диаметра

float z = (float)((diameter - nominalDiameter) / 10.0 * zPerDiameter);
// Для diameter=80: z = (80-70)/10*0.1 = 0.1 mm

// ШАГ 3: Настройте параметры слоя
MarkParameter[] layers = new MarkParameter[1];
layers[0] = new MarkParameter
{
    MarkSpeed = 800,    // из CLI
    LaserPower = 56.0f, // из CLI (280W / 500W * 100%)
    // ... другие параметры
};
HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

// ШАГ 4: Добавьте геометрию с Z-параметром
structUdmPos[] points = new structUdmPos[]
{
    new structUdmPos { x = -10, y = -10, z = z }, // ← Z применяет диаметр!
    new structUdmPos { x = 10, y = -10, z = z },
    new structUdmPos { x = 10, y = 10, z = z },
    new structUdmPos { x = -10, y = 10, z = z }
};

HM_UDM_DLL.UDM_AddPolyline3D(points, 4, 0); // ← 3D функция!

// ШАГ 5: Генерируйте и сохраняйте
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

**Готово!** Диаметр 80 μm применен через Z-смещение 0.1 мм.

---

## Почему через Z?

**Физика:**
```
Диаметр пучка на поверхности зависит от фокусного расстояния:
- Z = 0    → диаметр = 70 μm (номинальный)
- Z = +0.1 → диаметр = 80 μm (расфокусировка)
- Z = -0.1 → диаметр = 60 μm (фокусировка)
```

**В Hans API:**
- Параметр Z в `structUdmPos` контролирует положение фокуса
- Положение фокуса → изменение диаметра пятна
- Это СТАНДАРТНЫЙ способ контроля диаметра в лазерных системах

---

## Формула конвертации диаметра в Z

### Базовая формула:

```csharp
float DiameterToZ(double diameter, double nominalDiameter, double zPerDiameter)
{
    double delta = diameter - nominalDiameter;  // Изменение диаметра
    return (float)(delta / 10.0 * zPerDiameter); // Конвертация в Z
}

// Пример использования:
float z = DiameterToZ(
    diameter: 80.0,           // Целевой диаметр из CLI
    nominalDiameter: 70.0,    // Номинальный диаметр (из system.ini)
    zPerDiameter: 0.1         // Калибровочный коэффициент
);
// z = 0.1 mm
```

### Откуда брать параметры?

1. **`nominalDiameter`** - из конфигурации сканера `system.ini`:
   ```ini
   [Laser]
   FocusZ=0.0
   SpotSize=70  ← номинальный диаметр в μm
   ```

2. **`zPerDiameter`** - калибруется экспериментально:
   - Измерьте диаметр пятна при разных Z
   - Постройте график diameter(Z)
   - Найдите наклон: `ΔZ / Δdiameter`

---

## Полный пример из реального кода

```csharp
using Hans.NET;

public class ApplyDiameterFromCLI
{
    public static void ProcessLayer(double diameter, double power, int speed)
    {
        // 1. Инициализация
        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D!

        // 2. Диаметр → Z
        float z = (float)((diameter - 70.0) / 10.0 * 0.1);

        // 3. Параметры
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = (uint)speed,
            LaserPower = (float)(power / 500.0 * 100.0),
            JumpSpeed = 5000,
            MarkDelay = 100,
            JumpDelay = 100,
            PolygonDelay = 50,
            Frequency = 30.0f,
            DutyCycle = 0.5f
        };
        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        // 4. Геометрия (из CLI парсера)
        foreach (var polyline in geometries)
        {
            structUdmPos[] points = new structUdmPos[polyline.Count];
            for (int i = 0; i < polyline.Count; i++)
            {
                points[i] = new structUdmPos
                {
                    x = (float)polyline[i].X,
                    y = (float)polyline[i].Y,
                    z = z  // ← Диаметр применяется здесь!
                };
            }
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);
        }

        // 5. Сохранение
        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile($"layer_{layerNum}.bin");
        HM_UDM_DLL.UDM_EndMain();
    }
}
```

---

## Альтернативный способ: 3D коррекция

Если ваша система использует field curvature correction:

```csharp
// Установка коррекции (делается один раз при инициализации)
float baseFocal = 0.0f;
double[] paraK = new double[] { 0.0, 0.0, 0.001 }; // Коэффициенты A, B, C
HM_UDM_DLL.UDM_Set3dCorrectionPara(baseFocal, paraK, paraK.Length);

// Затем Hans автоматически применит коррекцию к Z
structUdmPos point = new structUdmPos { x = 5, y = 5, z = zForDiameter };
HM_UDM_DLL.UDM_AddPolyline3D(new[] { point }, 1, 0);
```

---

## Сравнение с Java API (из PrintMateMC)

### Java (PrintMateMC):
```java
// Высокоуровневый API
operations.add(new DiameterOperation(80.0));
scanner.loadOperations(producer);
```

### C# (Hans DLL):
```csharp
// Низкоуровневый API
float z = DiameterToZ(80.0);
structUdmPos point = new structUdmPos { x = 0, y = 0, z = z };
HM_UDM_DLL.UDM_AddPolyline3D(new[] { point }, 1, 0);
```

**В PrintMateMC** Java API (org.iiv.hlssystem) делает эту конвертацию **автоматически** внутри `DiameterOperation`. Вы работаете напрямую с DLL, поэтому конвертацию нужно делать вручную.

---

## Частые вопросы

### ❓ Нужно ли менять Z для каждой точки?

**Нет**, если диаметр постоянный:
```csharp
// Все точки с одним Z
float z = 0.1f;
structUdmPos[] points = new[]
{
    new structUdmPos { x = 0, y = 0, z = z },
    new structUdmPos { x = 10, y = 0, z = z },
    new structUdmPos { x = 10, y = 10, z = z }
};
```

**Да**, если диаметр меняется:
```csharp
// Разный диаметр = разный Z
points[0].z = DiameterToZ(80.0); // Диаметр 80
points[1].z = DiameterToZ(70.0); // Диаметр 70
```

### ❓ Что если я работаю в 2D режиме?

В 2D режиме диаметр контролируется только через:
1. Конфигурацию сканера (`system.ini`)
2. Физическую настройку оптики

Программно изменить диаметр в 2D **НЕЛЬЗЯ**.

### ❓ Как узнать коэффициент `zPerDiameter`?

1. **Из документации** вашей оптики
2. **Калибровка**:
   - Напечатайте тестовые линии с разным Z (-0.5 to +0.5 мм)
   - Измерьте ширину линий (микроскоп)
   - Постройте график
3. **Типичные значения**: 0.05 - 0.2 мм на 10 μm

---

## Резюме

### Вопрос: У меня diameter = 80 μm, как применить?

**Ответ:**
```csharp
// 1. Конвертируй в Z
float z = (float)((80.0 - 70.0) / 10.0 * 0.1); // = 0.1 mm

// 2. Включи 3D режим
HM_UDM_DLL.UDM_SetProtocol(0, 1);

// 3. Используй Z в геометрии
new structUdmPos { x = x, y = y, z = z }
HM_UDM_DLL.UDM_AddPolyline3D(points, count, layerIndex);
```

**Вот и всё!** 🎯

---

## Файлы примеров

- **HansNativeAPI_DiameterExample.cs** - полный рабочий код с 3 способами
- Запуск: `dotnet run` или компиляция в Visual Studio
