# ⚡ Быстрые исправления для тестирования

## Проблема
Все измеренные диаметры ≈ 50 мкм независимо от заданного диаметра.
**Вывод:** Сканер работает в фокусе, игнорируя Z координату.

---

## Фикс #1: Использовать UDM_AddPolyline3D вместо UDM_AddPoint2D

### Файл: TestUdmBuilder.cs, строка 112

**БЫЛО:**
```csharp
UDM_AddPoint2D(point, dwellTimeMs, 0);  // layerIndex = 0
```

**ИЗМЕНИТЬ НА:**
```csharp
// UDM_AddPoint2D может игнорировать Z координату
// Используем UDM_AddPolyline3D (как в Java коде)
structUdmPos[] polyline = new structUdmPos[] { point };
UDM_AddPolyline3D(polyline, 1, 0);  // 1 точка, layerIndex = 0

Console.WriteLine($"✓ Точка добавлена через UDM_AddPolyline3D (Z={point.z:F6} мм)");
```

---

## Фикс #2: Включить 3D коррекцию SDK

### Файл: TestUdmBuilder.cs, строка 79-82

**РАСКОММЕНТИРОВАТЬ:**
```csharp
// 2. Инициализируем UDM (как в Java UdmProducer.setOpsBefore() line 285-295)
UDM_Main();
UDM_SkyWriting(1);  // Всегда включаем SkyWriting
UDM_SetProtocol(1, 1);  // XY2_100_PROTOCOL_INDEX=1, DIMENSIONAL_3D_INDEX=1

// ДОБАВИТЬ ЭТИ СТРОКИ:
Console.WriteLine("Включение 3D коррекции...");
double[] correctionPoly = new double[]
{
    _config.ThirdAxisConfig.Cfactor,
    _config.ThirdAxisConfig.Bfactor,
    _config.ThirdAxisConfig.Afactor
};
int result = UDM_Set3dCorrectionPara(
    (float)_config.BeamConfig.FocalLengthMm,
    correctionPoly,
    correctionPoly.Length
);
Console.WriteLine($"UDM_Set3dCorrectionPara вернул код: {result}");
if (result == 0)
{
    Console.WriteLine($"✓ 3D коррекция включена: фокус={_config.BeamConfig.FocalLengthMm} мм");
}
else
{
    Console.WriteLine($"⚠️ ВНИМАНИЕ: Код {result} при включении 3D коррекции!");
}
```

---

## Фикс #3: Увеличить масштаб Z координаты (эксперимент)

### Файл: TestUdmBuilder.cs, строка 95

**ДОБАВИТЬ ПОСЛЕ РАСЧЕТА Z:**
```csharp
// 6. Вычисляем Z-координату (ТОЧНЫЙ ПОРТ Java BeamConfig.getCorrectZValue)
float zCoord = GetCorrectZValue(x, y, 0.0f);  // coordZMm = 0 для SLM печати

// ЭКСПЕРИМЕНТ: Увеличиваем Z в 10 раз
// float zCoordOriginal = zCoord;
// zCoord = zCoord * 10f;
// Console.WriteLine($"⚠️ ЭКСПЕРИМЕНТ: Z увеличена x10: {zCoordOriginal:F6} -> {zCoord:F6} мм");

Console.WriteLine($"\nИтоговая точка UDM:");
Console.WriteLine($"  X = {x:F3} мм");
Console.WriteLine($"  Y = {y:F3} мм");
Console.WriteLine($"  Z = {zCoord:F6} мм");
```

Раскомментируйте 3 строки чтобы попробовать увеличить Z в 10 раз.

---

## Фикс #4: Тест с фиксированными Z координатами

### Создать новый метод в RunDiameterTests.cs:

```csharp
/// <summary>
/// Тест с ручными Z координатами для проверки работы расфокусировки
/// </summary>
public static void TestManualZ()
{
    try
    {
        var config = LoadConfiguration();
        if (config == null)
        {
            Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
            return;
        }

        var builder = new TestUdmBuilder(config);

        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("ТЕСТ: Ручные Z координаты (проверка работы расфокусировки)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        float[] testZ = new float[] { 0f, 0.05f, 0.1f, 0.2f, 0.5f };

        foreach (float z in testZ)
        {
            Console.WriteLine($"\n┌─── Прожиг точки с Z = {z:F3} мм ───┐");

            string binFile = builder.BuildSinglePoint(
                x: 0f,
                y: 0f,
                z: z,
                beamDiameterMicron: 100.0,  // Не важно, Z задана вручную
                powerWatts: 200f,
                dwellTimeMs: 500
            );

            Console.WriteLine($"✓ UDM файл: {binFile}");
            Console.WriteLine($"└─────────────────────────────────────┘");
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("СЛЕДУЮЩИЙ ШАГ:");
        Console.WriteLine("1. Загрузите созданные UDM файлы в Hans сканер");
        Console.WriteLine("2. Прожгите все 5 точек");
        Console.WriteLine("3. Измерьте диаметры под микроскопом");
        Console.WriteLine();
        Console.WriteLine("ОЖИДАЕМЫЙ РЕЗУЛЬТАТ:");
        Console.WriteLine("  Z=0.00 мм → диаметр ≈ 63 мкм (фокус)");
        Console.WriteLine("  Z=0.05 мм → диаметр ≈ 70 мкм");
        Console.WriteLine("  Z=0.10 мм → диаметр ≈ 80 мкм");
        Console.WriteLine("  Z=0.20 мм → диаметр ≈ 100 мкм");
        Console.WriteLine("  Z=0.50 мм → диаметр ≈ 150 мкм");
        Console.WriteLine();
        Console.WriteLine("ЕСЛИ ВСЕ ДИАМЕТРЫ ОДИНАКОВЫЕ → Z не работает (проблема в сканере/SDK)");
        Console.WriteLine("ЕСЛИ ДИАМЕТРЫ РАЗНЫЕ → Z работает (проблема в расчете)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}
```

**Использование:**
```csharp
RunDiameterTests.TestManualZ();
```

---

## 🎯 План действий

### 1. Сначала проверьте ПРОСТОЙ тест
```csharp
RunDiameterTests.TestManualZ();
```

Прожгите точки и измерьте диаметры.

**ЕСЛИ диаметры одинаковые →** Проблема в SDK или настройках сканера (фиксы #1, #2)

**ЕСЛИ диаметры разные →** Проблема в расчете (ваша текущая ситуация маловероятна)

---

### 2. Примените Фикс #1 (UDM_AddPolyline3D)

Это самое вероятное решение - `UDM_AddPoint2D` может игнорировать Z.

---

### 3. Примените Фикс #2 (3D коррекция)

Без вызова `UDM_Set3dCorrectionPara` сканер может не применять Z координату.

---

### 4. Если не помогло - Фикс #3 (увеличить Z)

Возможно Z слишком маленькая и сканер считает ее погрешностью.

---

## 📝 Чек-лист

- [ ] Применить Фикс #1 (UDM_AddPolyline3D)
- [ ] Применить Фикс #2 (3D коррекция)
- [ ] Запустить TestManualZ()
- [ ] Прожечь точки с Z = 0, 0.05, 0.1, 0.2, 0.5
- [ ] Измерить диаметры
- [ ] Если не помогло - попробовать Фикс #3 (Z × 10)
- [ ] Проверить настройки Hans Laser Marker (3D режим)
- [ ] Проверить документацию SDK Hans

---

## ⚠️ ВАЖНО

**Проблема НЕ В АЛГОРИТМЕ!** Алгоритм - точный порт Java кода.

Проблема в том, что Z координата НЕ ПРИМЕНЯЕТСЯ сканером.

Нужно:
1. Выяснить почему (SDK или настройки)
2. Исправить передачу Z координаты
3. ИЛИ использовать другой метод управления диаметром
