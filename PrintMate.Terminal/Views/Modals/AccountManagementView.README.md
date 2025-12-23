# AccountManagementView - Модальное окно управления учетной записью

## Описание

Модальное окно для управления учетной записью пользователя. Предоставляет возможность:
- Просмотр данных профиля (имя, фамилия, логин)
- Смена пароля
- Выход из учетной записи

## Использование

### Открытие модального окна из ViewModel

```csharp
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Modals;
using PrintMate.Terminal.ViewModels.ModalsViewModels;

public class YourViewModel
{
    private readonly DialogService _dialogService;

    public YourViewModel(DialogService dialogService)
    {
        _dialogService = dialogService;
    }

    private void OpenAccountManagement()
    {
        // Показать модальное окно управления аккаунтом
        _dialogService.ShowDialog<AccountManagementView, AccountManagementViewModel>();
    }
}
```

### Открытие из Code-Behind (например, по клику на профиль)

```csharp
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Modals;
using PrintMate.Terminal.ViewModels.ModalsViewModels;

private void OnProfileClick(object sender, MouseButtonEventArgs e)
{
    var dialogService = Bootstrapper.ContainerProvider.Resolve<DialogService>();
    dialogService.ShowDialog<AccountManagementView, AccountManagementViewModel>();
}
```

### Пример в LeftBarView.xaml.cs

Если вы хотите добавить открытие окна управления аккаунтом при клике на профиль в левой панели:

```csharp
// В файле Views/LeftBarView.xaml.cs

using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Modals;
using PrintMate.Terminal.ViewModels.ModalsViewModels;

private void OnProfileClick(object sender, MouseButtonEventArgs e)
{
    var dialogService = Bootstrapper.ContainerProvider.Resolve<DialogService>();
    dialogService.ShowDialog<AccountManagementView, AccountManagementViewModel>();
}
```

Затем в XAML добавьте обработчик:

```xml
<Border MouseDown="OnProfileClick" Background="Black" Padding="20" ...>
    <!-- Ваш профиль пользователя -->
</Border>
```

## Функциональность

### 1. Отображение профиля
- Фамилия (только для чтения)
- Имя (только для чтения)
- Логин (только для чтения)

### 2. Смена пароля
- Кнопка "Сменить пароль" открывает форму смены пароля
- Форма содержит поля:
  - Текущий пароль (обязательное)
  - Новый пароль (обязательное)
  - Подтверждение пароля (обязательное, должно совпадать с новым)
- Валидация полей с красными предупреждениями
- Ограничения:
  - Нельзя сменить пароль root-профиля (Login="a", Password="a")
  - Пароли должны совпадать
  - Текущий пароль должен быть правильным

### 3. Выход из учетной записи
- Кнопка "Выйти из учетной записи" (красная)
- Подтверждение перед выходом
- При выходе:
  - Вызывается `AuthorizationService.Logout()`
  - Публикуется событие `OnUserQuit`
  - Модальное окно закрывается

## События

При выходе из системы публикуется событие:
- `OnUserQuit` - событие выхода пользователя (подписывайтесь для переадресации на экран логина)

## Зависимости

- `AuthorizationService` - управление авторизацией
- `UserService` - работа с пользователями в БД
- `DialogService` - отображение модального окна

## UI/UX особенности

- Раскрывающаяся форма смены пароля (toggle)
- Валидация с визуальной обратной связью (красные предупреждения)
- Темная тема в стиле приложения
- Использование компонента `EditboxKeyboard` для ввода текста
- Кнопка выхода выделена красным цветом для акцента

## Архитектура

**ViewModel:** [AccountManagementViewModel.cs](../../ViewModels/ModalsViewModels/AccountManagementViewModel.cs)
**View:** [AccountManagementView.xaml](AccountManagementView.xaml)
**Converters:**
- `BoolToVisibilityConverter` - для отображения/скрытия формы смены пароля
- `BoolToPasswordChangeTextConverter` - для текста кнопки toggle

Зарегистрировано в [Bootstrapper.cs:194](../../Bootstrapper.cs#L194)
