# Migration: DialogService → ModalService

## Обзор

Все вызовы `DialogService.Show()` были заменены на асинхронные вызовы `ModalService.ShowAsync()` для улучшения отзывчивости UI.

## ✅ Изменённые файлы

### 1. ViewModels

**`PrintMate.Terminal/ViewModels/ProjectsViewViewModel.cs`**
```csharp
// Было:
_dialogService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(new Dictionary<string, object>
{
    {"ProjectInfo", e}
});

// Стало:
await _modalService.ShowAsync<ProjectPreviewModal, ProjectPreviewModalViewModel>(
    options: new Dictionary<string, object>
    {
        {"ProjectInfo", e}
    },
    showOverlay: true,
    closeOnBackgroundClick: false  // Превью проекта - важное окно
);
```

**Изменения:**
- Метод `OnSelectProjectCallback` теперь асинхронный (`async void`)
- Используется `await _modalService.ShowAsync()` вместо `_dialogService.Show()`
- Добавлен параметр `closeOnBackgroundClick: false` для защиты от случайного закрытия

---

### 2. Views

**`PrintMate.Terminal/Views/ProjectsView.xaml.cs`**
```csharp
// Было:
private readonly DialogService _dialogService;
public ProjectsView(DialogService dialogService)
{
    _dialogService = dialogService;
}

private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
{
    _dialogService.Show<AddProjectWrapperView, AddProjectWrapperViewModel>();
}

// Стало:
private readonly ModalService _modalService;
public ProjectsView(ModalService modalService)
{
    _modalService = modalService;
}

private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
{
    await _modalService.ShowAsync<AddProjectWrapperView, AddProjectWrapperViewModel>();
}
```

**Изменения:**
- Заменён `DialogService` на `ModalService` в DI
- Event handler стал асинхронным
- Используется `await` для показа модального окна

---

**`PrintMate.Terminal/Views/RightBarView.xaml.cs`**
```csharp
// Было:
private readonly DialogService _dialogService;
public RightBarView(..., DialogService dialogService, ...)
{
    _dialogService = dialogService;
}

private void F_OnMouseDown(object sender, MouseButtonEventArgs e)
{
    var result = _dialogService.Show<RemoveUserForm, RemoveUserFormViewModel>(new()
    {
        {nameof(RemoveUserFormViewModel.Name), "Артём"},
    });
    if (result.IsSuccess) { ... }
}

// Стало:
private readonly ModalService _modalService;
public RightBarView(..., ModalService modalService, ...)
{
    _modalService = modalService;
}

private async void F_OnMouseDown(object sender, MouseButtonEventArgs e)
{
    var result = await _modalService.ShowAsync<RemoveUserForm, RemoveUserFormViewModel>(
        options: new Dictionary<string, object>
        {
            {nameof(RemoveUserFormViewModel.Name), "Артём"}
        }
    );
    if (result.IsSuccess) { ... }
}
```

**Изменения:**
- Заменён `DialogService` на `ModalService` в DI
- Event handler стал асинхронным
- Используется явный `Dictionary<string, object>` вместо `new()`

---

## 📊 Статистика миграции

| Файл | Тип | Метод | Изменения |
|------|-----|-------|-----------|
| ProjectsViewViewModel.cs | ViewModel | OnSelectProjectCallback | async void + await |
| ProjectsView.xaml.cs | View | ButtonBase_OnClick | async void + await |
| RightBarView.xaml.cs | View | F_OnMouseDown | async void + await |

**Всего изменено:** 3 файла
**Всего вызовов:** 3 вызова DialogService → ModalService

---

## 🎯 Преимущества изменений

### До миграции (DialogService):
- ❌ UI блокировался при открытии модального окна
- ❌ Пользователь не мог взаимодействовать с приложением
- ❌ Создавались новые Window объекты

### После миграции (ModalService):
- ✅ UI остаётся отзывчивым
- ✅ Плавные анимации появления/скрытия
- ✅ Использование Canvas overlay (меньше накладных расходов)
- ✅ Опциональная защита от закрытия кликом по фону
- ✅ Асинхронный подход (async/await)

---

## 🔍 Детали реализации

### closeOnBackgroundClick параметр

```csharp
// Для важных модалок (например, превью проекта)
closeOnBackgroundClick: false  // Закрывать только через кнопку

// Для простых модалок (по умолчанию)
closeOnBackgroundClick: true   // Можно закрыть кликом по фону
```

### Асинхронные event handlers

```csharp
// Event handlers могут быть async void
private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
{
    // Важно использовать await!
    await _modalService.ShowAsync<MyView, MyViewModel>();

    // Этот код выполнится ПОСЛЕ закрытия модалки
    Console.WriteLine("Модалка закрыта");
}
```

---

## ⚠️ Важные замечания

### 1. Использование await обязательно
```csharp
// ❌ НЕПРАВИЛЬНО - забыли await
_modalService.ShowAsync<MyView, MyViewModel>();
DoSomething(); // Выполнится ДО закрытия модалки!

// ✅ ПРАВИЛЬНО
await _modalService.ShowAsync<MyView, MyViewModel>();
DoSomething(); // Выполнится ПОСЛЕ закрытия модалки
```

### 2. async void только для event handlers
```csharp
// ✅ OK для event handlers
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await _modalService.ShowAsync<...>();
}

// ✅ Лучше для обычных методов
private async Task ShowModalAsync()
{
    await _modalService.ShowAsync<...>();
}
```

### 3. DI инъекция
Все Views теперь получают `ModalService` вместо `DialogService` через конструктор:
```csharp
public MyView(ModalService modalService)
{
    _modalService = modalService;
}
```

---

## 🚀 Следующие шаги

### Опциональная миграция CustomMessageBox

Уже выполнено! Все вызовы `CustomMessageBox` используют `ModalService` через асинхронные методы:
- `ShowInformationAsync()`
- `ShowWarningAsync()`
- `ShowErrorAsync()`
- `ShowSuccessAsync()`
- `ShowQuestionAsync()`
- `ShowConfirmationAsync()`

Старые синхронные методы сохранены для обратной совместимости.

---

## ✅ Проверка

Проект успешно скомпилирован без ошибок:
```bash
dotnet build PrintMate.Terminal\PrintMate.Terminal.csproj --configuration Debug
# Сборка успешно завершена.
```

---

## 📚 См. также

- [ModalService.cs](PrintMate.Terminal/Services/ModalService.cs) - реализация сервиса
- [MODAL_SERVICE_README.md](PrintMate.Terminal/Services/MODAL_SERVICE_README.md) - документация
- [CUSTOM_MESSAGEBOX_MIGRATION.md](PrintMate.Terminal/Services/CUSTOM_MESSAGEBOX_MIGRATION.md) - миграция MessageBox
- [MODAL_SYSTEM_UPDATE.md](MODAL_SYSTEM_UPDATE.md) - общий обзор системы
