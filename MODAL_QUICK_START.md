# ModalService - Краткая шпаргалка

## 🚀 Быстрый старт

### 1. Показать модалку (fire-and-forget)

```csharp
// Простейший случай - показать и забыть
_modalService.Show<HelpModal, HelpViewModel>();

// С параметрами
_modalService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(
    options: new Dictionary<string, object>
    {
        {"ProjectInfo", projectData}
    }
);

// С именованным ID
_modalService.Show<ProgressModal, ProgressViewModel>(
    modalId: "ProgressIndicator",
    options: new Dictionary<string, object>
    {
        {"Title", "Загрузка..."}
    },
    closeOnBackgroundClick: false
);
```

### 2. Закрыть модалку

```csharp
// Из ViewModel модалки
ModalService.Instance.Close();

// По ID
ModalService.Instance.Close("ProgressIndicator");

// С результатом
ModalService.Instance.Close(isSuccess: true);

// Из кнопки
private void CloseButton_Click(object sender, RoutedEventArgs e)
{
    ModalService.Instance.Close();
}
```

### 3. Показать с ожиданием результата (async)

```csharp
// Только когда НУЖЕН результат!
private async void CreateUser()
{
    var result = await _modalService.ShowAsync<AddUserForm, AddUserFormViewModel>();

    if (result.Result.IsCreated)
    {
        MessageBox.Show($"Пользователь {result.Result.Login} создан");
        Users.Add(result.Result.User);
    }
}
```

---

## 📖 Примеры из кода

### Превью проекта (ProjectsViewViewModel.cs)

```csharp
private async void OnSelectProjectCallback(object e)
{
    var result = await CustomMessageBox.ShowConfirmationAsync(
        "Выбрать проект?",
        "Будет предоставлена детальная информация и превью проекта."
    );

    if (result == MessageBoxResult.Yes)
    {
        // Fire-and-forget
        _modalService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(
            options: new Dictionary<string, object>
            {
                {"ProjectInfo", e}
            },
            showOverlay: true,
            closeOnBackgroundClick: false
        );
    }
}
```

### Добавление проекта (ProjectsView.xaml.cs)

```csharp
private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
{
    _modalService.Show<AddProjectWrapperView, AddProjectWrapperViewModel>();
}
```

### Создание пользователя с результатом (ConfigureParametersUsersViewModel.cs)

```csharp
private async void CreateUser()
{
    var result = await modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>();

    if (result.Result.IsCreated)
    {
        MessageBox.Show($"Пользователь {result.Result.Login} добавлен");
        Users.Add(result.Result.Returned);
        CheckUsersCount();
    }
}
```

### Закрытие из ViewModel (MessageBoxViewModel.cs)

```csharp
private void OnOk()
{
    Result = Models.MessageBoxResult.OK;
    ModalService.Instance.Close();
}
```

---

## 🎯 Типичные сценарии

### Индикатор прогресса

```csharp
// Показываем
_modalService.Show<ProgressModal, ProgressViewModel>(
    modalId: "ImportProgress",
    closeOnBackgroundClick: false
);

// Выполняем работу
await DoLongRunningTask();

// Закрываем
ModalService.Instance.Close("ImportProgress");
```

### Уведомление с автозакрытием

```csharp
public class NotificationViewModel : BindableBase
{
    public NotificationViewModel()
    {
        // Автозакрытие через 3 секунды
        Task.Delay(3000).ContinueWith(_ =>
        {
            ModalService.Instance.Close();
        });
    }
}
```

### Множественные модалки

```csharp
// Открываем несколько
var id1 = _modalService.Show<Modal1, ViewModel1>();
var id2 = _modalService.Show<Modal2, ViewModel2>();

// Закрываем конкретную
ModalService.Instance.Close(id1);

// Или последнюю
ModalService.Instance.Close();
```

### Singleton модалка

```csharp
private const string SETTINGS_ID = "AppSettings";

public void ToggleSettings()
{
    try
    {
        _modalService.Show<SettingsModal, SettingsViewModel>(
            modalId: SETTINGS_ID
        );
    }
    catch (InvalidOperationException)
    {
        // Уже открыто - закрываем
        ModalService.Instance.Close(SETTINGS_ID);
    }
}
```

---

## ⚡ Параметры методов

### Show / ShowAsync

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `modalId` | `string` | `null` | ID модалки (null = автоген) |
| `options` | `Dictionary<string, object>` | `null` | Параметры для ViewModel |
| `showOverlay` | `bool` | `true` | Затемнённый фон |
| `closeOnBackgroundClick` | `bool` | `true` | Закрытие кликом |

### Close

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `modalId` | `string` | `null` | ID модалки (null = последняя) |
| `isSuccess` | `bool` | `true` | Успешное закрытие |

---

## 🔀 Когда что использовать?

| Задача | Метод | Пример |
|--------|-------|--------|
| Показать информацию | `Show()` | Помощь, превью |
| Уведомление | `Show()` | Ошибка, успех |
| Прогресс | `Show()` | Загрузка |
| Форма с выбором | `ShowAsync()` | OK/Cancel |
| Создание/удаление | `ShowAsync()` | Добавить пользователя |
| Выбор из списка | `ShowAsync()` | Выбор файла |

---

## 🐛 Частые ошибки

### ❌ Забыли await

```csharp
// ПЛОХО - результат потеряется!
var result = _modalService.ShowAsync<MyView, MyViewModel>();
if (result.Result.IsOk) { ... } // ERROR!
```

**✅ Исправление:**
```csharp
var result = await _modalService.ShowAsync<MyView, MyViewModel>();
if (result.Result.IsOk) { ... } // OK
```

### ❌ Используете await когда не нужно

```csharp
// ПЛОХО - зачем ждать если результат не нужен?
await _modalService.ShowAsync<HelpModal, HelpViewModel>();
```

**✅ Исправление:**
```csharp
_modalService.Show<HelpModal, HelpViewModel>();
```

### ❌ Дублирование ID

```csharp
// ПЛОХО - вторая модалка не откроется!
_modalService.Show<Modal1, ViewModel1>(modalId: "MyId");
_modalService.Show<Modal2, ViewModel2>(modalId: "MyId"); // Exception!
```

**✅ Исправление:**
```csharp
_modalService.Show<Modal1, ViewModel1>(modalId: "Modal1");
_modalService.Show<Modal2, ViewModel2>(modalId: "Modal2");
```

---

## 📚 Дополнительные материалы

- **[MODAL_SERVICE_USAGE.md](MODAL_SERVICE_USAGE.md)** - Подробное руководство
- **[MODAL_CUSTOM_IDS.md](MODAL_CUSTOM_IDS.md)** - Работа с именованными ID
- **[COMPLETE_MIGRATION_REPORT.md](COMPLETE_MIGRATION_REPORT.md)** - Отчёт о миграции
- **[ModalService.cs](PrintMate.Terminal/Services/ModalService.cs)** - Исходный код
