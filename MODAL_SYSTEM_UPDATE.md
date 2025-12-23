# Modal System Update - ModalService & CustomMessageBox

## 🎉 Обзор изменений

Реализована новая асинхронная система модальных окон на базе Canvas overlay, которая **не блокирует UI поток** в отличие от старого подхода с `Window.ShowDialog()`.

## 📦 Что было добавлено

### 1. ModalService - Асинхронный сервис модальных окон

**Файл:** `PrintMate.Terminal/Services/ModalService.cs`

Современный сервис для отображения модальных окон:
- ✅ Асинхронный API через `async/await`
- ✅ Работает через Canvas overlay (ZIndex: 500-501)
- ✅ Плавные анимации появления/скрытия
- ✅ Опциональный затемнённый фон
- ✅ Закрытие кликом по фону (опционально)
- ✅ Поддержка стека модальных окон
- ✅ Интеграция с Prism DI

**Основной метод:**
```csharp
var result = await _modalService.ShowAsync<MyView, MyViewModel>(
    options: parameters,
    showOverlay: true,
    closeOnBackgroundClick: true
);
```

### 2. Обновлённый CustomMessageBox

**Файл:** `PrintMate.Terminal/Services/CustomMessageBox.cs`

Переработан для использования `ModalService`:
- ✅ Новые асинхронные методы: `ShowInformationAsync()`, `ShowWarningAsync()`, и т.д.
- ✅ Старые синхронные методы сохранены для обратной совместимости
- ✅ MessageBox нельзя закрыть кликом по фону
- ✅ UI поток не блокируется

**Примеры:**
```csharp
// Новый асинхронный способ (рекомендуется)
var result = await CustomMessageBox.ShowQuestionAsync("Удалить?", "Вы уверены?");

// Старый синхронный способ (работает, но не рекомендуется)
var result = CustomMessageBox.ShowQuestion("Удалить?", "Вы уверены?");
```

### 3. Обновления в MainWindow

**Файлы:**
- `PrintMate.Terminal/Views/MainWindow.xaml` - добавлены Canvas элементы
- `PrintMate.Terminal/Views/MainWindow.xaml.cs` - инициализация ModalService

Добавлены два новых Canvas элемента:
```xml
<Canvas x:Name="ModalOverlay" Panel.ZIndex="500"/>  <!-- Затемнённый фон -->
<Canvas x:Name="ModalContainer" Panel.ZIndex="501"/> <!-- Модальный контент -->
```

### 4. Регистрация в DI контейнере

**Файл:** `PrintMate.Terminal/Bootstrapper.cs`

```csharp
containerRegistry.RegisterSingleton<ModalService>();
```

### 5. Обновлённый MessageBoxViewModel

**Файл:** `PrintMate.Terminal/ViewModels/ModalsViewModels/MessageBoxViewModel.cs`

Добавлена поддержка закрытия через `ModalService` с fallback на `DialogService`.

### 6. Документация

Созданы подробные руководства:

**`PrintMate.Terminal/Services/MODAL_SERVICE_README.md`**
- API Reference для ModalService
- Примеры использования
- Сравнение с DialogService
- Troubleshooting

**`PrintMate.Terminal/Services/CUSTOM_MESSAGEBOX_MIGRATION.md`**
- Руководство по миграции с синхронных методов на асинхронные
- Примеры для каждого типа MessageBox
- Checklist миграции
- Решение типичных проблем

### 7. Примеры использования

**`PrintMate.Terminal/ViewModels/ExampleModalUsageViewModel.cs`**
- Примеры работы с ModalService

**`PrintMate.Terminal/ViewModels/ExampleCustomMessageBoxViewModel.cs`**
- Демонстрация всех типов MessageBox
- Примеры workflow с последовательными MessageBox

## 🚀 Быстрый старт

### Использование ModalService

```csharp
public class MyViewModel : BindableBase
{
    private readonly ModalService _modalService;

    public MyViewModel(ModalService modalService)
    {
        _modalService = modalService;
    }

    private async Task ShowModal()
    {
        var result = await _modalService.ShowAsync<MyView, MyViewModel>();

        if (result.IsSuccess)
        {
            // Пользователь закрыл модалку успешно
        }
    }
}
```

### Использование CustomMessageBox

```csharp
// Асинхронный способ (рекомендуется)
private async Task DeleteFileAsync()
{
    var result = await CustomMessageBox.ShowQuestionAsync(
        "Удалить файл?",
        "Это действие нельзя отменить"
    );

    if (result == MessageBoxResult.Yes)
    {
        await _fileService.DeleteAsync();
        await CustomMessageBox.ShowSuccessAsync("Готово", "Файл удалён");
    }
}
```

## 📊 Сравнение старого и нового подходов

| Характеристика | DialogService (старый) | ModalService (новый) |
|----------------|------------------------|----------------------|
| Блокирует UI | ✅ Да (ShowDialog) | ❌ Нет (async/await) |
| Отзывчивость UI | ❌ UI зависает | ✅ UI работает |
| Анимации | ✅ Есть | ✅ Есть (улучшенные) |
| Затемнение фона | ✅ Есть | ✅ Есть (с анимацией) |
| Закрытие кликом | ❌ Нет | ✅ Опционально |
| Технология | Window | Canvas Overlay |
| Z-Index управление | Owner/Child | Stack с auto Z-Index |
| Производительность | Создаёт Window | Переиспользует Canvas |
| API стиль | Синхронный | Асинхронный |

## 🔄 Миграция существующего кода

### Шаг 1: Найдите все использования CustomMessageBox
```bash
# В IDE поиск по файлам
CustomMessageBox.Show
```

### Шаг 2: Замените на асинхронные версии
```csharp
// Было:
private void OnDelete()
{
    var result = CustomMessageBox.ShowQuestion("Удалить?", "Подтверждение");
    if (result == MessageBoxResult.Yes) { ... }
}

// Стало:
private async Task OnDelete()  // или async void для event handlers
{
    var result = await CustomMessageBox.ShowQuestionAsync("Удалить?", "Подтверждение");
    if (result == MessageBoxResult.Yes) { ... }
}
```

### Шаг 3: Обновите команды Prism
```csharp
// Было:
public DelegateCommand SaveCommand { get; }

public MyViewModel()
{
    SaveCommand = new DelegateCommand(Save);
}

private void Save()
{
    var result = CustomMessageBox.ShowQuestion(...);
}

// Стало:
public DelegateCommand SaveCommand { get; }

public MyViewModel()
{
    SaveCommand = new DelegateCommand(async () => await SaveAsync());
}

private async Task SaveAsync()
{
    var result = await CustomMessageBox.ShowQuestionAsync(...);
}
```

## ⚠️ Важные замечания

### Обратная совместимость
Все старые синхронные методы **сохранены** и продолжат работать:
```csharp
// Это работает, но блокирует UI (не рекомендуется)
CustomMessageBox.ShowWarning("Внимание", "Старый код");
```

### Когда использовать синхронные методы?
- В legacy коде, который сложно переписать
- В конструкторах (где async не поддерживается)
- В синхронных методах третьих библиотек

### Избегайте deadlocks!
```csharp
// ❌ ОЧЕНЬ ПЛОХО - может вызвать deadlock!
var result = CustomMessageBox.ShowQuestionAsync(...).Result;

// ✅ ПРАВИЛЬНО - используйте синхронный метод
var result = CustomMessageBox.ShowQuestion(...);

// ✅ ИЛИ ЕЩЁ ЛУЧШЕ - сделайте метод асинхронным
var result = await CustomMessageBox.ShowQuestionAsync(...);
```

## 🎨 Визуальные изменения

### ModalService
- Модальное окно отображается в центре Canvas
- Плавное затемнение фона (opacity 0 → 1)
- Анимация появления: масштаб 0.7 → 1.0 + сдвиг сверху
- Анимация скрытия: масштаб 1.0 → 0.8 + сдвиг вниз
- Easing функции для плавности (BackEase, ExponentialEase)

### CustomMessageBox
- Сохранены все иконки и стили
- Нельзя закрыть кликом по фону (безопасность)
- Overlay обязателен (showOverlay: true)

## 📁 Структура файлов

```
PrintMate.Terminal/
├── Services/
│   ├── ModalService.cs                          # Новый асинхронный сервис
│   ├── CustomMessageBox.cs                      # Обновлённый (async + backward compat)
│   ├── DialogService.cs                         # Старый сервис (сохранён)
│   ├── MODAL_SERVICE_README.md                  # Документация ModalService
│   └── CUSTOM_MESSAGEBOX_MIGRATION.md           # Руководство по миграции
├── ViewModels/
│   ├── ExampleModalUsageViewModel.cs            # Примеры ModalService
│   ├── ExampleCustomMessageBoxViewModel.cs      # Примеры CustomMessageBox
│   └── ModalsViewModels/
│       └── MessageBoxViewModel.cs               # Обновлён для ModalService
├── Views/
│   ├── MainWindow.xaml                          # Добавлены Canvas элементы
│   └── MainWindow.xaml.cs                       # Инициализация ModalService
└── Bootstrapper.cs                              # Регистрация ModalService
```

## ✅ Проверка работоспособности

Проект успешно скомпилирован:
```bash
dotnet build PrintMate.Terminal\PrintMate.Terminal.csproj --configuration Debug
# Сборка успешно завершена.
```

## 🔜 Рекомендации по дальнейшему использованию

1. **Новый код** - используйте `ModalService` и асинхронные методы `CustomMessageBox`
2. **Legacy код** - можно оставить синхронные методы, но постепенно мигрировать
3. **Критичные операции** - для операций требующих строгой блокировки UI можно использовать `DialogService`
4. **Тестирование** - протестируйте все сценарии использования MessageBox

## 📚 Дополнительная информация

- `MODAL_SERVICE_README.md` - подробная документация API
- `CUSTOM_MESSAGEBOX_MIGRATION.md` - руководство по миграции
- `ExampleModalUsageViewModel.cs` - примеры использования
- `ExampleCustomMessageBoxViewModel.cs` - демонстрация всех типов MessageBox

---

**Автор:** Claude Code
**Дата:** 2025
**Версия:** 1.0
