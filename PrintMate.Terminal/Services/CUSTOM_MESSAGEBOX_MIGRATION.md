# CustomMessageBox Migration Guide

## Обзор изменений

`CustomMessageBox` был обновлён для использования нового асинхронного `ModalService` вместо блокирующего `DialogService`. Все существующие методы сохранены для обратной совместимости.

## ✅ Что изменилось?

### До (старый код):
```csharp
// Блокирует UI поток
var result = CustomMessageBox.ShowWarning("Внимание", "Файл будет удалён");
if (result == MessageBoxResult.Yes)
{
    DeleteFile();
}
```

### После (новый код):
```csharp
// НЕ блокирует UI поток - использует async/await
var result = await CustomMessageBox.ShowWarningAsync("Внимание", "Файл будет удалён");
if (result == MessageBoxResult.Yes)
{
    DeleteFile();
}
```

## 🔄 Миграция кода

### Шаг 1: Добавьте `async` к методу
```csharp
// Было:
private void OnDeleteClick()
{
    var result = CustomMessageBox.ShowQuestion("Удалить?", "Вы уверены?");
    // ...
}

// Стало:
private async Task OnDeleteClick()  // или async void для event handlers
{
    var result = await CustomMessageBox.ShowQuestionAsync("Удалить?", "Вы уверены?");
    // ...
}
```

### Шаг 2: Замените методы на асинхронные версии

| Старый метод (синхронный) | Новый метод (асинхронный) |
|---------------------------|---------------------------|
| `ShowInformation()` | `ShowInformationAsync()` |
| `ShowWarning()` | `ShowWarningAsync()` |
| `ShowError()` | `ShowErrorAsync()` |
| `ShowSuccess()` | `ShowSuccessAsync()` |
| `ShowQuestion()` | `ShowQuestionAsync()` |
| `ShowConfirmation()` | `ShowConfirmationAsync()` |
| `Show()` | `ShowAsync()` |

## 📖 Примеры использования

### Пример 1: Информационное сообщение
```csharp
private async Task ShowInfoAsync()
{
    await CustomMessageBox.ShowInformationAsync(
        "Информация",
        "Операция завершена успешно"
    );
}
```

### Пример 2: Подтверждение действия
```csharp
private async Task DeleteProjectAsync()
{
    var result = await CustomMessageBox.ShowQuestionAsync(
        "Удалить проект?",
        "Это действие нельзя отменить"
    );

    if (result == MessageBoxResult.Yes)
    {
        await _projectService.DeleteAsync();
        await CustomMessageBox.ShowSuccessAsync("Готово", "Проект удалён");
    }
}
```

### Пример 3: Обработка ошибок
```csharp
private async Task SaveDataAsync()
{
    try
    {
        await _repository.SaveAsync();
    }
    catch (Exception ex)
    {
        await CustomMessageBox.ShowErrorAsync(
            "Ошибка сохранения",
            $"Не удалось сохранить данные: {ex.Message}"
        );
    }
}
```

### Пример 4: В команде Prism
```csharp
public class MyViewModel : BindableBase
{
    public DelegateCommand SaveCommand { get; }

    public MyViewModel()
    {
        SaveCommand = new DelegateCommand(async () => await SaveAsync());
    }

    private async Task SaveAsync()
    {
        var result = await CustomMessageBox.ShowQuestionAsync(
            "Сохранить изменения?",
            "Вы внесли изменения в конфигурацию"
        );

        if (result == MessageBoxResult.Yes)
        {
            await SaveConfiguration();
            await CustomMessageBox.ShowSuccessAsync("Успех", "Конфигурация сохранена");
        }
    }
}
```

### Пример 5: В Event Handler
```csharp
private async void OnCloseButtonClick(object sender, RoutedEventArgs e)
{
    var result = await CustomMessageBox.ShowQuestionAsync(
        "Закрыть окно?",
        "Несохранённые данные будут потеряны"
    );

    if (result == MessageBoxResult.Yes)
    {
        Close();
    }
}
```

### Пример 6: Произвольные параметры
```csharp
private async Task ShowCustomMessageAsync()
{
    var result = await CustomMessageBox.ShowAsync(
        title: "Выберите действие",
        message: "Что вы хотите сделать?",
        type: MessageBoxType.YesNoCancel,
        icon: MessageBoxIcon.Question
    );

    switch (result)
    {
        case MessageBoxResult.Yes:
            await SaveAndClose();
            break;
        case MessageBoxResult.No:
            Close();
            break;
        case MessageBoxResult.Cancel:
            // Ничего не делаем
            break;
    }
}
```

## ⚠️ Обратная совместимость

Все старые синхронные методы **сохранены** и продолжат работать:

```csharp
// Это всё ещё работает, но НЕ рекомендуется (блокирует UI)
var result = CustomMessageBox.ShowWarning("Внимание", "Старый код");
```

### Когда можно использовать синхронные методы?
- В legacy коде, который сложно переписать
- В конструкторах (где нельзя использовать async)
- В синхронных методах, которые нельзя сделать async

⚠️ **Важно**: Синхронные методы блокируют UI поток и могут вызвать зависание интерфейса!

## 🎨 Визуальные отличия

### ModalService (новый):
- ✅ Отображается в Canvas overlay
- ✅ Плавные анимации появления/скрытия
- ✅ UI остаётся отзывчивым
- ✅ Затемнённый фон с анимацией
- ✅ Нельзя закрыть кликом по фону (для MessageBox)

### DialogService (старый):
- ❌ Создаёт новое Window
- ❌ Блокирует UI поток
- ❌ Анимации есть, но UI заморожен

## 🔧 Устранение проблем

### Проблема: "Cannot await in synchronous method"
```csharp
// ❌ Неправильно
private void MyMethod()
{
    var result = await CustomMessageBox.ShowWarningAsync(...); // Ошибка!
}

// ✅ Правильно
private async Task MyMethod()
{
    var result = await CustomMessageBox.ShowWarningAsync(...);
}

// ✅ Или для event handlers
private async void MyMethod()
{
    var result = await CustomMessageBox.ShowWarningAsync(...);
}
```

### Проблема: "Forgot to await"
```csharp
// ❌ Неправильно - забыли await
CustomMessageBox.ShowWarningAsync("Внимание", "Сообщение");
DoSomething(); // Выполнится ДО закрытия MessageBox!

// ✅ Правильно
await CustomMessageBox.ShowWarningAsync("Внимание", "Сообщение");
DoSomething(); // Выполнится ПОСЛЕ закрытия MessageBox
```

### Проблема: "Deadlock in synchronous code"
```csharp
// ❌ ОЧЕНЬ плохо - может вызвать deadlock!
private void SyncMethod()
{
    var result = CustomMessageBox.ShowWarningAsync(...).Result; // НЕ ДЕЛАЙТЕ ТАК!
}

// ✅ Используйте синхронный метод вместо этого
private void SyncMethod()
{
    var result = CustomMessageBox.ShowWarning(...); // Для обратной совместимости
}

// ✅ Или лучше - сделайте метод асинхронным
private async Task AsyncMethod()
{
    var result = await CustomMessageBox.ShowWarningAsync(...);
}
```

## 📊 Checklist миграции

- [ ] Замените `ShowInformation()` на `ShowInformationAsync()`
- [ ] Замените `ShowWarning()` на `ShowWarningAsync()`
- [ ] Замените `ShowError()` на `ShowErrorAsync()`
- [ ] Замените `ShowSuccess()` на `ShowSuccessAsync()`
- [ ] Замените `ShowQuestion()` на `ShowQuestionAsync()`
- [ ] Замените `ShowConfirmation()` на `ShowConfirmationAsync()`
- [ ] Добавьте `async Task` или `async void` к методам
- [ ] Добавьте `await` перед вызовами
- [ ] Протестируйте все сценарии использования
- [ ] Убедитесь, что UI не блокируется

## 🚀 Преимущества миграции

✅ **Отзывчивый UI** - интерфейс не зависает во время показа MessageBox
✅ **Современный код** - следует best practices async/await
✅ **Лучший UX** - плавные анимации и затемнение
✅ **Производительность** - использует Canvas вместо Window
✅ **Гибкость** - легче расширять и кастомизировать

## См. также

- [ModalService.cs](ModalService.cs) - новый асинхронный сервис
- [MODAL_SERVICE_README.md](MODAL_SERVICE_README.md) - документация ModalService
- [DialogService.cs](DialogService.cs) - старый синхронный сервис
