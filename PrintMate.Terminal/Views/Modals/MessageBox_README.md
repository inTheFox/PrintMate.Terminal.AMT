# Custom MessageBox

Красивая система MessageBox для PrintMate.Terminal с темной темой и анимациями.

## Возможности

- ✅ Темная тема в стиле приложения
- ✅ 5 типов иконок (Information, Warning, Error, Success, Question)
- ✅ 4 типа наборов кнопок (OK, YesNo, OKCancel, YesNoCancel)
- ✅ Анимированное появление/исчезновение
- ✅ Адаптивная высота под длину текста
- ✅ Прокрутка для длинных сообщений
- ✅ Центрирование относительно главного окна

## Использование

### Быстрые методы

```csharp
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Models;

// Информационное сообщение
CustomMessageBox.ShowInformation(
    "Информация",
    "Проект успешно загружен!"
);

// Предупреждение
CustomMessageBox.ShowWarning(
    "Предупреждение",
    "Некоторые файлы отсутствуют."
);

// Ошибка
CustomMessageBox.ShowError(
    "Ошибка",
    "Не удалось подключиться к PLC."
);

// Успех
CustomMessageBox.ShowSuccess(
    "Успешно",
    "Операция завершена успешно!"
);

// Вопрос с кнопками Да/Нет
var result = CustomMessageBox.ShowQuestion(
    "Вопрос",
    "Вы уверены, что хотите продолжить?"
);

if (result == MessageBoxResult.Yes)
{
    // Пользователь нажал "Да"
}

// Подтверждение с кнопками Да/Нет
var result = CustomMessageBox.ShowConfirmation(
    "Подтверждение",
    "Удалить выбранный проект?"
);

if (result == MessageBoxResult.Yes)
{
    // Удаляем проект
}
```

### Продвинутое использование

```csharp
// Кастомный MessageBox с произвольными параметрами
var result = CustomMessageBox.Show(
    title: "Внимание",
    message: "Это важное сообщение с выбором действия.",
    type: MessageBoxType.YesNoCancel,
    icon: MessageBoxIcon.Warning
);

switch (result)
{
    case MessageBoxResult.Yes:
        // Действие "Да"
        break;
    case MessageBoxResult.No:
        // Действие "Нет"
        break;
    case MessageBoxResult.Cancel:
        // Действие "Отмена"
        break;
}
```

## Типы MessageBox

### MessageBoxType (наборы кнопок)

- **OK** - только кнопка OK
- **YesNo** - кнопки Да и Нет
- **OKCancel** - кнопки OK и Отмена
- **YesNoCancel** - кнопки Да, Нет и Отмена

### MessageBoxIcon (иконки)

| Иконка | Цвет | Применение |
|--------|------|------------|
| **None** | - | Без иконки |
| **Information** | Синий (#2196F3) | Информационные сообщения |
| **Warning** | Оранжевый (#FF9800) | Предупреждения |
| **Error** | Красный (#F44336) | Ошибки |
| **Question** | Синий (#2196F3) | Вопросы пользователю |
| **Success** | Зелёный (#4CAF50) | Успешные операции |

### MessageBoxResult (результат)

- **None** - окно закрыто без выбора
- **Yes** - нажата кнопка "Да"
- **No** - нажата кнопка "Нет"
- **OK** - нажата кнопка "OK"
- **Cancel** - нажата кнопка "Отмена"

## Примеры использования в проекте

### Подтверждение удаления проекта

```csharp
var result = CustomMessageBox.ShowQuestion(
    "Удаление проекта",
    $"Вы действительно хотите удалить проект \"{project.Name}\"?\n\nЭто действие нельзя отменить."
);

if (result == MessageBoxResult.Yes)
{
    await _projectRepository.DeleteAsync(project);
    CustomMessageBox.ShowSuccess("Успешно", "Проект удалён.");
}
```

### Уведомление об ошибке парсинга

```csharp
try
{
    var project = await _cncProvider.ParseAsync(filePath);
}
catch (Exception ex)
{
    CustomMessageBox.ShowError(
        "Ошибка парсинга",
        $"Не удалось распарсить файл:\n\n{ex.Message}"
    );
}
```

### Предупреждение о несохранённых изменениях

```csharp
if (HasUnsavedChanges)
{
    var result = CustomMessageBox.Show(
        "Несохранённые изменения",
        "У вас есть несохранённые изменения. Сохранить перед выходом?",
        MessageBoxType.YesNoCancel,
        MessageBoxIcon.Question
    );

    switch (result)
    {
        case MessageBoxResult.Yes:
            SaveChanges();
            CloseWindow();
            break;
        case MessageBoxResult.No:
            CloseWindow();
            break;
        case MessageBoxResult.Cancel:
            // Отменяем закрытие
            break;
    }
}
```

### Информация о версии

```csharp
CustomMessageBox.ShowInformation(
    "О программе",
    $"PrintMate.Terminal\nВерсия: {AppVersion}\n\n© 2025 Все права защищены"
);
```

## Дизайн

- **Фон**: Тёмно-серый (#1a1a1a)
- **Заголовок**: Тёмный (#252525)
- **Тени**: Мягкая тень с размытием 30px
- **Скругление углов**: 10px
- **Размер**: 600x250-600 (адаптивная высота)
- **Шрифты**:
  - Заголовок: 24pt Bold
  - Текст: 18pt Regular
  - Кнопки: 16pt Bold

### Цвета кнопок

- **OK/Да**: Зелёный (#4CAF50)
- **Нет**: Красный (#F44336)
- **Отмена**: Серый (#757575)

## Анимации

- Fade in при открытии (opacity 0 → 1)
- Scale in для содержимого (0.9 → 1.0)
- Easing: CubicEase с EaseOut

## Интеграция

MessageBox автоматически:
- Центрируется относительно главного окна
- Блокирует взаимодействие с главным окном (модальный режим)
- Закрывается при нажатии любой кнопки
- Возвращает результат выбора пользователя

## Требования

- .NET 9.0
- WPF
- Prism.DryIoc
- HandyControl (для команд)

## Архитектура

```
Models/
  └── MessageBoxResult.cs      - Enums для типов и результатов

ViewModels/ModalsViewModels/
  └── MessageBoxViewModel.cs   - ViewModel с логикой

Views/Modals/
  ├── MessageBoxView.xaml      - XAML разметка
  └── MessageBoxView.xaml.cs   - Code-behind

Services/
  └── CustomMessageBox.cs      - Статический хелпер для вызова
```
