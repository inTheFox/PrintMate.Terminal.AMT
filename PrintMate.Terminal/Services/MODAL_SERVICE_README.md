# ModalService - Асинхронные модальные окна

## Обзор

`ModalService` — это современная альтернатива `DialogService`, которая **не блокирует UI поток**. Модальные окна отображаются через Canvas overlay в MainWindow, используя анимации и асинхронный подход.

## Основные преимущества

✅ **Асинхронность** - UI поток не блокируется, приложение остаётся отзывчивым
✅ **Анимации** - плавное появление/скрытие с затемнением фона
✅ **Гибкость** - можно показывать несколько модалок одновременно
✅ **Контроль** - полная кастомизация поведения (overlay, клик по фону и т.д.)
✅ **Интеграция с Prism** - работает с DI и MVVM паттернами

## Быстрый старт

### 1. Инъекция сервиса

```csharp
public class MyViewModel : BindableBase
{
    private readonly ModalService _modalService;

    public MyViewModel(ModalService modalService)
    {
        _modalService = modalService;
    }
}
```

### 2. Базовое использование

```csharp
// Простое открытие модалки
var result = await _modalService.ShowAsync<MyView, MyViewModel>();

if (result.IsSuccess)
{
    Console.WriteLine("Модалка закрыта успешно!");
    var viewModel = result.Result; // Доступ к ViewModel
}
else
{
    Console.WriteLine("Модалка отменена пользователем");
}
```

## Примеры использования

### Пример 1: Передача параметров в ViewModel

```csharp
var options = new Dictionary<string, object>
{
    { "Username", "admin" },
    { "IsEditMode", true },
    { "UserId", 123 }
};

var result = await _modalService.ShowAsync<EditUserView, EditUserViewModel>(options);
```

ViewModel получит эти параметры через свойства:

```csharp
public class EditUserViewModel : BindableBase
{
    public string Username { get; set; }
    public bool IsEditMode { get; set; }
    public int UserId { get; set; }
}
```

### Пример 2: Модалка без затемнённого фона

```csharp
var result = await _modalService.ShowAsync<NotificationView, NotificationViewModel>(
    options: null,
    showOverlay: false,  // Без затемнения
    closeOnBackgroundClick: false  // Нельзя закрыть кликом по фону
);
```

### Пример 3: Последовательное открытие модалок

```csharp
// Открываем первую модалку
var step1 = await _modalService.ShowAsync<SelectProjectView, SelectProjectViewModel>();
if (!step1.IsSuccess) return;

// Открываем вторую модалку
var step2 = await _modalService.ShowAsync<ConfigureProjectView, ConfigureProjectViewModel>();
if (!step2.IsSuccess) return;

// Открываем третью модалку
var step3 = await _modalService.ShowAsync<ConfirmView, ConfirmViewModel>();
```

### Пример 4: Закрытие из ViewModel

Если ваш ViewModel реализует `IViewModelForm`:

```csharp
public class MyViewModel : BindableBase, IViewModelForm
{
    public ICommand CloseCommand { get; set; }  // Устанавливается ModalService

    public ICommand SaveCommand { get; }

    public MyViewModel()
    {
        SaveCommand = new DelegateCommand(() =>
        {
            // Сохраняем данные...

            // Закрываем модалку
            CloseCommand?.Execute(null);
        });
    }
}
```

### Пример 5: Программное закрытие

```csharp
// Закрыть последнее модальное окно
await _modalService.CloseAsync();

// Закрыть с флагом успеха/отмены
await _modalService.CloseAsync(modalId: null, isSuccess: true);
```

### Пример 6: Получение WindowId в ViewModel

```csharp
public class MyViewModel : BindableBase
{
    public string WindowId { get; set; }  // Автоматически устанавливается ModalService

    public async Task CloseThisModal()
    {
        // Закрываем конкретное модальное окно по ID
        await ModalService.Instance.CloseAsync(WindowId, isSuccess: true);
    }
}
```

## API Reference

### ShowAsync<ViewType, ViewModelType>

Открывает модальное окно асинхронно.

**Параметры:**
- `options` (Dictionary<string, object>) - параметры для инициализации ViewModel
- `showOverlay` (bool) - показывать ли затемнённый фон (default: true)
- `closeOnBackgroundClick` (bool) - закрывать ли при клике на фон (default: true)

**Возвращает:**
- `Task<ModalResult<ViewModelType>>` - результат с флагом успеха и ViewModel

### CloseAsync

Закрывает модальное окно асинхронно.

**Параметры:**
- `modalId` (string) - ID модального окна (null = последнее)
- `isSuccess` (bool) - успешное ли закрытие (default: true)

**Возвращает:**
- `Task` - завершается после анимации закрытия

### Initialize

Инициализирует сервис с Canvas контейнерами (вызывается автоматически в MainWindow).

**Параметры:**
- `modalContainer` (Canvas) - контейнер для модального контента
- `backgroundOverlay` (Canvas) - контейнер для затемнённого фона

## Сравнение с DialogService

| Функция | DialogService | ModalService |
|---------|---------------|--------------|
| Блокирует UI поток | ✅ Да (ShowDialog) | ❌ Нет (async/await) |
| Анимации | ✅ Есть | ✅ Есть |
| Затемнённый фон | ✅ Есть | ✅ Есть (опционально) |
| Несколько модалок | ✅ Стек окон | ✅ Стек в Canvas |
| Закрытие кликом | ❌ Нет | ✅ Опционально |
| Производительность | Создаёт новые Window | Переиспользует Canvas |
| Интеграция с Prism | ✅ Да | ✅ Да |

## Когда использовать ModalService vs DialogService?

### Используйте ModalService когда:
- ✅ Нужна отзывчивость UI во время показа модалки
- ✅ Требуются сложные анимации и кастомизация
- ✅ Модалка должна закрываться кликом по фону
- ✅ Нужно показывать несколько модалок одновременно
- ✅ Важна производительность (Canvas вместо Window)

### Используйте DialogService когда:
- ✅ Нужно строгое блокирование UI (критичные операции)
- ✅ Модалка должна быть отдельным Window (например, для multi-monitor setup)
- ✅ Требуется совместимость с существующим кодом

## Архитектура

```
MainWindow (Grid)
├── RootRegion (основной контент)
├── CanvasBackground (существующие overlay)
├── LoadingScreenCanvas
├── NotificationContainer
├── ModalOverlay (затемнённый фон, ZIndex: 500)
└── ModalContainer (модальный контент, ZIndex: 501)
    ├── Modal 1 (ZIndex: 200)
    ├── Modal 2 (ZIndex: 202)
    └── Modal N (ZIndex: 200 + N*2)
```

## Внутреннее устройство

1. **ModalContext** - хранит состояние каждой модалки (View, ViewModel, TaskCompletionSource)
2. **_modalStack** - стек открытых модалок для управления порядком
3. **_zIndexCounter** - автоинкрементный счётчик для правильного наложения
4. **AnimateShow/Hide** - анимации с использованием ScaleTransform + TranslateTransform
5. **TaskCompletionSource** - обеспечивает асинхронность через async/await

## Troubleshooting

### Модалка не отображается
- Убедитесь, что `ModalService.Initialize()` вызывается в MainWindow
- Проверьте, что Canvas элементы `ModalContainer` и `ModalOverlay` присутствуют в XAML

### Анимация не работает
- Проверьте, что View является UIElement (UserControl, Grid и т.д.)
- Убедитесь, что не переопределён RenderTransform в XAML

### ViewModel не получает параметры
- Свойства в ViewModel должны быть public и иметь setter
- Имена в Dictionary должны совпадать с именами свойств (регистронезависимо)

### Клик по фону не закрывает модалку
- Убедитесь, что `closeOnBackgroundClick: true` (по умолчанию)
- Проверьте, что ModalOverlay имеет Background (не Transparent)

## См. также

- [ExampleModalUsageViewModel.cs](../ViewModels/ExampleModalUsageViewModel.cs) - полные примеры использования
- [DialogService.cs](DialogService.cs) - старый сервис с Window.ShowDialog()
- [NotificationService.cs](NotificationService.cs) - сервис для toast уведомлений
