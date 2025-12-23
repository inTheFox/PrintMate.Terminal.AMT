# Пользовательские ID для модальных окон

## Обзор

Теперь вы можете указывать **собственный ID** для модального окна вместо автоматически генерируемого GUID. Это удобно для:

- Именованных модалок (например, `"ProgressModal"`, `"SettingsDialog"`)
- Управления несколькими однотипными модалками
- Явного закрытия конкретной модалки без сохранения переменной
- Проверки, открыта ли уже модалка с таким ID

---

## 📌 Базовое использование

### 1. Автоматическая генерация ID (по умолчанию)

```csharp
// ID будет сгенерирован автоматически (Guid.NewGuid())
string modalId = _modalService.Show<HelpModal, HelpViewModel>();

Console.WriteLine(modalId); // "a7f3e8d2-4c5b-..."
```

### 2. Пользовательский ID

```csharp
// Используем свой ID
_modalService.Show<ProgressModal, ProgressViewModel>(
    modalId: "ProgressIndicator",
    options: new Dictionary<string, object>
    {
        {"Title", "Загрузка..."}
    }
);

// Закрываем по имени
ModalService.Instance.Close("ProgressIndicator");
```

---

## 🎯 Практические примеры

### Пример 1: Индикатор прогресса с именованным ID

```csharp
public class ProjectImportService
{
    private readonly ModalService _modalService;
    private const string PROGRESS_MODAL_ID = "ProjectImportProgress";

    public async Task ImportProjectAsync(string path)
    {
        // Показываем прогресс с фиксированным ID
        _modalService.Show<ProgressModal, ProgressViewModel>(
            modalId: PROGRESS_MODAL_ID,
            options: new Dictionary<string, object>
            {
                {"Title", "Импорт проекта"},
                {"Message", "Обработка файлов..."}
            },
            closeOnBackgroundClick: false
        );

        try
        {
            await _projectManager.ImportAsync(path);

            // Закрываем по имени
            ModalService.Instance.Close(PROGRESS_MODAL_ID);

            // Показываем успех
            _modalService.Show<SuccessModal, SuccessViewModel>(
                modalId: "ImportSuccess"
            );
        }
        catch (Exception ex)
        {
            ModalService.Instance.Close(PROGRESS_MODAL_ID);

            _modalService.Show<ErrorModal, ErrorViewModel>(
                modalId: "ImportError",
                options: new Dictionary<string, object>
                {
                    {"ErrorMessage", ex.Message}
                }
            );
        }
    }
}
```

### Пример 2: Множественные уведомления

```csharp
public class NotificationService
{
    private readonly ModalService _modalService;
    private int _notificationCounter = 0;

    public void ShowNotification(string message, NotificationType type)
    {
        // Генерируем уникальный ID для каждого уведомления
        var notificationId = $"Notification_{_notificationCounter++}";

        _modalService.Show<NotificationModal, NotificationViewModel>(
            modalId: notificationId,
            options: new Dictionary<string, object>
            {
                {"Message", message},
                {"Type", type},
                {"AutoCloseDelay", 5000}
            },
            showOverlay: false,
            closeOnBackgroundClick: true
        );

        // Автозакрытие через 5 секунд
        Task.Delay(5000).ContinueWith(_ =>
        {
            ModalService.Instance.Close(notificationId);
        });
    }

    public void CloseAllNotifications()
    {
        // Закрываем все уведомления по префиксу
        for (int i = 0; i < _notificationCounter; i++)
        {
            try
            {
                ModalService.Instance.Close($"Notification_{i}");
            }
            catch
            {
                // Модалка уже закрыта - игнорируем
            }
        }
    }
}
```

### Пример 3: Singleton-модалка (только одна может быть открыта)

```csharp
public class SettingsViewModel
{
    private readonly ModalService _modalService;
    private const string SETTINGS_MODAL_ID = "AppSettings";

    public void OpenSettings()
    {
        try
        {
            // Пытаемся открыть настройки с фиксированным ID
            _modalService.Show<SettingsModal, SettingsViewModel>(
                modalId: SETTINGS_MODAL_ID,
                showOverlay: true,
                closeOnBackgroundClick: true
            );
        }
        catch (InvalidOperationException ex)
        {
            // Модалка уже открыта - просто фокусируемся на ней
            Console.WriteLine("Настройки уже открыты");
            // Можно добавить анимацию "встряски" для привлечения внимания
        }
    }

    public void CloseSettings()
    {
        ModalService.Instance.Close(SETTINGS_MODAL_ID);
    }
}
```

### Пример 4: Управление связанными модалками

```csharp
public class WizardViewModel
{
    private readonly ModalService _modalService;
    private const string WIZARD_PREFIX = "Wizard_";

    public void ShowWizard()
    {
        // Шаг 1
        _modalService.Show<WizardStep1Modal, WizardStep1ViewModel>(
            modalId: $"{WIZARD_PREFIX}Step1",
            options: new Dictionary<string, object>
            {
                {"OnNext", (Action)ShowStep2}
            }
        );
    }

    private void ShowStep2()
    {
        // Закрываем шаг 1
        ModalService.Instance.Close($"{WIZARD_PREFIX}Step1");

        // Показываем шаг 2
        _modalService.Show<WizardStep2Modal, WizardStep2ViewModel>(
            modalId: $"{WIZARD_PREFIX}Step2",
            options: new Dictionary<string, object>
            {
                {"OnBack", (Action)ShowWizard},
                {"OnNext", (Action)ShowStep3}
            }
        );
    }

    private void ShowStep3()
    {
        ModalService.Instance.Close($"{WIZARD_PREFIX}Step2");

        _modalService.Show<WizardStep3Modal, WizardStep3ViewModel>(
            modalId: $"{WIZARD_PREFIX}Step3",
            options: new Dictionary<string, object>
            {
                {"OnBack", (Action)ShowStep2},
                {"OnFinish", (Action)CloseWizard}
            }
        );
    }

    public void CloseWizard()
    {
        // Закрываем все шаги мастера
        ModalService.Instance.Close($"{WIZARD_PREFIX}Step1");
        ModalService.Instance.Close($"{WIZARD_PREFIX}Step2");
        ModalService.Instance.Close($"{WIZARD_PREFIX}Step3");
    }
}
```

### Пример 5: Проверка существования модалки

```csharp
public class AppViewModel
{
    private const string HELP_MODAL_ID = "HelpWindow";

    public void ToggleHelp()
    {
        try
        {
            // Пытаемся открыть
            _modalService.Show<HelpModal, HelpViewModel>(
                modalId: HELP_MODAL_ID
            );
        }
        catch (InvalidOperationException)
        {
            // Уже открыто - закрываем
            ModalService.Instance.Close(HELP_MODAL_ID);
        }
    }
}
```

---

## 🔒 Защита от дублирования

Если вы попытаетесь открыть модалку с уже существующим ID, будет выброшено исключение:

```csharp
// Открываем первую модалку
_modalService.Show<MyModal, MyViewModel>(modalId: "MyUniqueId");

// Попытка открыть ещё одну с тем же ID
try
{
    _modalService.Show<MyModal, MyViewModel>(modalId: "MyUniqueId");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
    // "Модальное окно с ID 'MyUniqueId' уже открыто.
    //  Используйте уникальный ID или оставьте null для автогенерации."
}
```

---

## 📋 Рекомендации по именованию ID

### ✅ Хорошие примеры:

```csharp
// Константы для часто используемых модалок
private const string PROGRESS_MODAL = "ProgressIndicator";
private const string SETTINGS_MODAL = "AppSettings";
private const string ERROR_MODAL = "ErrorDialog";

// Префиксы для однотипных модалок
string notificationId = $"Notification_{timestamp}";
string wizardStepId = $"Wizard_Step{stepNumber}";
string projectPreviewId = $"ProjectPreview_{projectId}";
```

### ❌ Плохие примеры:

```csharp
// Слишком короткие
modalId: "1"
modalId: "m"

// Неинформативные
modalId: "modal"
modalId: "window"

// Конфликтующие
modalId: "Modal1"  // что это за модалка?
```

---

## 🎨 Паттерны использования

### Паттерн 1: Named Singleton

```csharp
// Одна модалка на всё приложение
private const string SINGLETON_ID = "UniqueModalId";

public void Show()
{
    _modalService.Show<MyModal, MyViewModel>(modalId: SINGLETON_ID);
}

public void Close()
{
    ModalService.Instance.Close(SINGLETON_ID);
}
```

### Паттерн 2: Auto-increment

```csharp
private int _modalCounter = 0;

public void ShowModal()
{
    var id = $"Modal_{_modalCounter++}";
    _modalService.Show<MyModal, MyViewModel>(modalId: id);
}
```

### Паттерн 3: GUID + Prefix

```csharp
public void ShowModal()
{
    var id = $"Notification_{Guid.NewGuid()}";
    _modalService.Show<MyModal, MyViewModel>(modalId: id);
}
```

### Паттерн 4: Context-based ID

```csharp
public void ShowProjectPreview(ProjectInfo project)
{
    // ID основан на контексте
    var id = $"ProjectPreview_{project.Id}";

    _modalService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(
        modalId: id,
        options: new Dictionary<string, object>
        {
            {"ProjectInfo", project}
        }
    );
}
```

---

## 🔧 API Reference

### Show (Fire-and-Forget)

```csharp
public string Show<ViewType, ViewModelType>(
    string modalId = null,                      // Пользовательский ID (null = автогенерация)
    Dictionary<string, object> options = null,  // Параметры для ViewModel
    bool showOverlay = true,                    // Показывать затемнение
    bool closeOnBackgroundClick = true          // Закрывать кликом по фону
)
```

### ShowAsync (с ожиданием результата)

```csharp
public Task<ModalResult<ViewModelType>> ShowAsync<ViewType, ViewModelType>(
    string modalId = null,                      // Пользовательский ID (null = автогенерация)
    Dictionary<string, object> options = null,  // Параметры для ViewModel
    bool showOverlay = true,                    // Показывать затемнение
    bool closeOnBackgroundClick = true          // Закрывать кликом по фону
)
```

### Close

```csharp
public void Close(
    string modalId = null,    // ID модалки (null = закрыть последнюю)
    bool isSuccess = true     // Успешное ли закрытие
)
```

---

## 📊 Сравнение подходов

| Подход | Плюсы | Минусы | Когда использовать |
|--------|-------|--------|-------------------|
| **Auto GUID** | Всегда уникально | Нужно сохранять переменную | Одноразовые модалки |
| **Named ID** | Легко ссылаться | Риск дублирования | Singleton модалки |
| **Prefix + Counter** | Уникально + читаемо | Нужен счётчик | Множественные однотипные |
| **Context-based** | Связано с данными | Может дублироваться | Модалки по сущностям |

---

## 💡 Советы

1. **Используйте константы** для часто используемых ID
2. **Префиксы помогают** группировать модалки по типам
3. **Try-catch** для обработки дубликатов ID
4. **null для одноразовых** модалок, именованные для переиспользуемых
5. **Документируйте ID** если используете их в нескольких местах

---

## 🚀 Миграция существующего кода

### Было (без именованных ID):

```csharp
// Сохраняли ID в переменную
string progressModalId = _modalService.Show<ProgressModal, ProgressViewModel>();

// ... позже ...
ModalService.Instance.Close(progressModalId);
```

### Стало (с именованными ID):

```csharp
// Используем константу
_modalService.Show<ProgressModal, ProgressViewModel>(
    modalId: PROGRESS_MODAL_ID
);

// ... позже, из любого места ...
ModalService.Instance.Close(PROGRESS_MODAL_ID);
```

---

## 📚 См. также

- [MODAL_SERVICE_USAGE.md](MODAL_SERVICE_USAGE.md) - Полное руководство по ModalService
- [ModalService.cs](PrintMate.Terminal/Services/ModalService.cs) - Исходный код
