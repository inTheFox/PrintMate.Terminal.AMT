# ModalService - Fire-and-Forget Mode

## Обзор

`ModalService` теперь поддерживает **два режима работы**:

1. **Fire-and-Forget** (`Show()`) - Показать модалку и сразу вернуться, не дожидаясь закрытия
2. **Async/Await** (`ShowAsync()`) - Дождаться закрытия и получить результат

---

## 🔥 Fire-and-Forget Mode (Рекомендуется)

### Когда использовать:
- Когда результат модального окна не нужен
- Когда модалка закрывается сама (например, превью проекта)
- Когда не нужно ждать действий пользователя

### Пример 1: Показать модалку без ожидания

```csharp
public class MyViewModel
{
    private readonly ModalService _modalService;

    public MyViewModel(ModalService modalService)
    {
        _modalService = modalService;
    }

    private void OnShowPreview(object parameter)
    {
        // Просто показываем окно - НЕ ЖДЁМ его закрытия
        _modalService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(
            options: new Dictionary<string, object>
            {
                {"ProjectInfo", parameter}
            },
            showOverlay: true,
            closeOnBackgroundClick: false
        );

        // Код продолжает выполнение сразу после показа окна
        Console.WriteLine("Модалка показана, но мы не ждём её закрытия!");
    }
}
```

### Пример 2: Закрыть модалку из любого места

```csharp
// Из ViewModel модального окна
public class ProjectPreviewModalViewModel
{
    public void OnProjectLoaded()
    {
        // Закрыть модалку (fire-and-forget)
        ModalService.Instance.Close();
    }
}

// Или из кнопки
private void CloseButton_Click(object sender, RoutedEventArgs e)
{
    ModalService.Instance.Close();
}

// Или из любого сервиса
public class MyService
{
    public void DoSomething()
    {
        // Закрыть текущую модалку
        ModalService.Instance.Close();
    }
}
```

### Пример 3: Закрыть конкретную модалку по ID

```csharp
// Сохраняем ID при открытии
string modalId = _modalService.Show<MyView, MyViewModel>();

// Позже закрываем именно эту модалку
ModalService.Instance.Close(modalId);
```

---

## ⏳ Async/Await Mode

### Когда использовать:
- Когда НУЖЕН результат после закрытия модалки
- Когда нужно выполнить действие после закрытия
- Когда пользователь должен сделать выбор (OK/Cancel, Yes/No)

### Пример 1: Дождаться результата

```csharp
private async void CreateUser()
{
    // Ждём, пока пользователь закроет модалку
    var result = await _modalService.ShowAsync<AddUserForm, AddUserFormViewModel>();

    // Обрабатываем результат ПОСЛЕ закрытия
    if (result.Result.IsCreated)
    {
        MessageBox.Show($"Пользователь {result.Result.Login} добавлен");
        Users.Add(result.Result.Returned);
    }
    else
    {
        MessageBox.Show("Отменено");
    }
}
```

### Пример 2: Цепочка модалок

```csharp
private async void ShowWizard()
{
    // Шаг 1: Выбор проекта
    var step1 = await _modalService.ShowAsync<Step1View, Step1ViewModel>();
    if (!step1.IsSuccess) return;

    // Шаг 2: Настройки (только если шаг 1 успешен)
    var step2 = await _modalService.ShowAsync<Step2View, Step2ViewModel>(
        options: new Dictionary<string, object>
        {
            {"ProjectData", step1.Result.SelectedProject}
        }
    );
    if (!step2.IsSuccess) return;

    // Шаг 3: Подтверждение
    await _modalService.ShowAsync<ConfirmView, ConfirmViewModel>();
}
```

---

## 📊 Сравнение режимов

| Аспект | Fire-and-Forget (`Show()`) | Async/Await (`ShowAsync()`) |
|--------|---------------------------|----------------------------|
| **Блокирует поток** | ❌ Нет | ❌ Нет (async) |
| **Возвращает результат** | ❌ Нет (только ID) | ✅ Да (`ModalResult<T>`) |
| **Ожидание закрытия** | ❌ Нет | ✅ Да (через `await`) |
| **Когда использовать** | Не нужен результат | Нужен результат |
| **Сложность кода** | ✅ Проще | Сложнее (async/await) |
| **Производительность** | ✅ Быстрее | Немного медленнее |

---

## 🎯 Практические советы

### 1. Выбор режима

```csharp
// ✅ Fire-and-Forget - просто показать окно
_modalService.Show<HelpModal, HelpViewModel>();

// ✅ Async/Await - нужен выбор пользователя
var result = await _modalService.ShowAsync<ConfirmModal, ConfirmViewModel>();
if (result.IsSuccess) { /* действие */ }
```

### 2. Закрытие из ViewModel

```csharp
public class MyModalViewModel
{
    public void OnSaveClick()
    {
        // Сохраняем данные
        SaveData();

        // Закрываем модалку (fire-and-forget)
        ModalService.Instance.Close();
    }
}
```

### 3. Закрытие с результатом

```csharp
public class UserFormViewModel
{
    public string Login { get; set; }
    public bool IsCreated { get; set; }

    public void OnOkClick()
    {
        IsCreated = true;

        // Закрытие с успехом (isSuccess: true)
        ModalService.Instance.Close(isSuccess: true);

        // ModalResult.Result будет содержать этот ViewModel
    }

    public void OnCancelClick()
    {
        IsCreated = false;

        // Закрытие с отменой (isSuccess: false)
        ModalService.Instance.Close(isSuccess: false);
    }
}
```

### 4. Множественные модалки

```csharp
// Открываем несколько модалок одновременно
string modal1 = _modalService.Show<Modal1, ViewModel1>();
string modal2 = _modalService.Show<Modal2, ViewModel2>();

// Закрываем конкретную
ModalService.Instance.Close(modal1);

// Или закрываем последнюю открытую (LIFO)
ModalService.Instance.Close();
```

---

## 🔧 Технические детали

### TaskCompletionSource

- При `Show()` - **не создаётся** TaskCompletionSource (экономия памяти)
- При `ShowAsync()` - создаётся TaskCompletionSource для ожидания

### Z-Index управление

- Каждая новая модалка получает увеличенный Z-Index (+2)
- Overlay имеет Z-Index на 1 меньше контента модалки
- При закрытии Z-Index освобождается

### Анимации

- Появление: 400ms (ScaleTransform + TranslateTransform + Opacity)
- Скрытие: 250ms (обратная анимация)
- `OnOpenAnimationFinish` событие вызывается после завершения анимации

---

## 🚀 Миграция с DialogService

### Было (DialogService - БЛОКИРУЕТ UI):

```csharp
// ❌ Блокирует UI поток!
_dialogService.Show<MyView, MyViewModel>();
// Код ждёт закрытия окна
```

### Стало (ModalService - НЕ БЛОКИРУЕТ):

```csharp
// ✅ Fire-and-Forget - не блокирует
_modalService.Show<MyView, MyViewModel>();
// Код продолжает выполнение сразу

// ✅ Или с ожиданием результата (если нужно)
var result = await _modalService.ShowAsync<MyView, MyViewModel>();
if (result.IsSuccess) { /* обработка */ }
```

---

## 📖 См. также

- [COMPLETE_MIGRATION_REPORT.md](COMPLETE_MIGRATION_REPORT.md) - Полный отчёт о миграции
- [ModalService.cs](PrintMate.Terminal/Services/ModalService.cs) - Исходный код
- [MODAL_SERVICE_README.md](PrintMate.Terminal/Services/MODAL_SERVICE_README.md) - Подробная документация API
