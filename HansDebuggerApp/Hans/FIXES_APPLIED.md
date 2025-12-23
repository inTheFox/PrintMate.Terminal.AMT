# Исправленные ошибки компиляции

## ✅ Все ошибки исправлены! Проект компилируется успешно.

### Исправление 1: TestUdmBuilder.cs (строка 290)

**Ошибка:**
```
error CS1503: Аргумент 1: не удается преобразовать из "double" в "float"
```

**Код до исправления:**
```csharp
double powerOffsetMicrons = _config.BeamConfig.GetPowerOffset(_currentPowerWatts, _config.LaserPowerConfig.MaxPower);
```

**Код после исправления:**
```csharp
double powerOffsetMicrons = _config.BeamConfig.GetPowerOffset((float)_currentPowerWatts, _config.LaserPowerConfig.MaxPower);
```

---

### Исправление 2: RunDiameterTests.cs (строка 91)

**Ошибка:**
```
error CS0117: "ScanatorConfigurationLoader" не содержит определение для "LoadFromJson"
```

**Код до исправления:**
```csharp
string json = File.ReadAllText(configPath);
var configs = ScanatorConfigurationLoader.LoadFromJson(json);
```

**Код после исправления:**
```csharp
var configs = ScanatorConfigurationLoader.LoadFromFile(configPath);
```

---

### Исправление 3: MainWindowViewModel.cs (строки 237-245, 252-260)

**Ошибки:**
```
error CS0117: "TestUdmBuilder" не содержит определение для "BaseFocal"
error CS0117: "TestUdmBuilder" не содержит определение для "ZOffsetMm"
error CS0117: "TestUdmBuilder" не содержит определение для "PowerWatts"
error CS0117: "TestUdmBuilder" не содержит определение для "CorrectedPowerWatts"
error CS0117: "TestUdmBuilder" не содержит определение для "PowerPercent"
error CS0266: Не удается неявно преобразовать тип "double" в "float"
```

**Причина:**
Эти статические поля были удалены из `TestUdmBuilder` при переписывании на Java порт.

**Решение:**
Закомментированы строки с удаленными полями и добавлено явное приведение типов для оставшихся полей.

**Код до исправления:**
```csharp
FocalLengthMm = TestUdmBuilder.FocalLengthMm;
FocalLengthMicron = TestUdmBuilder.FocalLengthMicron;
BaseFocal = TestUdmBuilder.BaseFocal;
ZOffsetMm = TestUdmBuilder.ZOffsetMm;
PowerOffsetMicrons = TestUdmBuilder.PowerOffsetMicrons;
ZFinal = TestUdmBuilder.ZFinal;
PowerWatts = TestUdmBuilder.PowerWatts;
CorrectedPowerWatts = TestUdmBuilder.CorrectedPowerWatts;
PowerPercent = TestUdmBuilder.PowerPercent;
```

**Код после исправления:**
```csharp
FocalLengthMm = (float)TestUdmBuilder.FocalLengthMm;
FocalLengthMicron = (float)TestUdmBuilder.FocalLengthMicron;
// BaseFocal = TestUdmBuilder.BaseFocal;  // Удалено из TestUdmBuilder
// ZOffsetMm = TestUdmBuilder.ZOffsetMm;  // Удалено из TestUdmBuilder
PowerOffsetMicrons = (float)TestUdmBuilder.PowerOffsetMicrons;
ZFinal = (float)TestUdmBuilder.ZFinal;
// PowerWatts = TestUdmBuilder.PowerWatts;  // Удалено из TestUdmBuilder
// CorrectedPowerWatts = TestUdmBuilder.CorrectedPowerWatts;  // Удалено из TestUdmBuilder
// PowerPercent = TestUdmBuilder.PowerPercent;  // Удалено из TestUdmBuilder
```

---

## 📊 Результат компиляции

```
Сборка успешно завершена.
    Предупреждений: 0
    Ошибок: 0

Прошло времени 00:00:00.87
```

## ✅ Доступные статические поля в TestUdmBuilder

После исправлений доступны только эти поля:

```csharp
public static double FocalLengthMm;        // Базовое фокусное расстояние (мм)
public static double FocalLengthMicron;    // После всех коррекций (мкм)
public static double LensTravelMicron;     // Смещение линзы от диаметра (мкм)
public static double PowerOffsetMicrons;   // Смещение от мощности (мкм)
public static double ZFinal;               // Итоговая Z координата (мм)
```

**Удаленные поля** (которые были в старой версии):
- ~~`BaseFocal`~~ - теперь берется из конфига
- ~~`ZOffsetMm`~~ - не нужен, используется LensTravelMicron
- ~~`PowerWatts`~~ - не нужен для отладки
- ~~`CorrectedPowerWatts`~~ - не нужен для отладки
- ~~`PowerPercent`~~ - не нужен для отладки

---

## 🚀 Теперь можно запускать!

```csharp
// В App.xaml.cs или где угодно
using HansDebuggerApp.Hans;

// Быстрый тест
RunDiameterTests.RunQuick();

// Полный набор тестов
RunDiameterTests.RunAll();
```

Все исправлено и готово к работе! ✅
