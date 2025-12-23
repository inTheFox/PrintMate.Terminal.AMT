using PrintMate.Terminal.Services;
using PrintMate.Terminal.Models;

namespace PrintMate.Terminal.Examples
{
    /// <summary>
    /// Примеры использования CustomMessageBox
    /// </summary>
    public class MessageBoxExamples
    {
        public void ShowExamples()
        {
            // ============================================
            // 1. ИНФОРМАЦИОННОЕ СООБЩЕНИЕ
            // ============================================
            CustomMessageBox.ShowInformation(
                "Загрузка завершена",
                "Проект успешно загружен и готов к печати."
            );

            // ============================================
            // 2. ПРЕДУПРЕЖДЕНИЕ
            // ============================================
            CustomMessageBox.ShowWarning(
                "Внимание",
                "Температура камеры превышает допустимые значения."
            );

            // ============================================
            // 3. ОШИБКА
            // ============================================
            CustomMessageBox.ShowError(
                "Ошибка подключения",
                "Не удалось подключиться к PLC.\nПроверьте сетевое соединение."
            );

            // ============================================
            // 4. УСПЕШНАЯ ОПЕРАЦИЯ
            // ============================================
            CustomMessageBox.ShowSuccess(
                "Готово",
                "Печать успешно завершена!"
            );

            // ============================================
            // 5. ВОПРОС С ВЫБОРОМ ДА/НЕТ
            // ============================================
            var result = CustomMessageBox.ShowQuestion(
                "Подтверждение",
                "Начать печать проекта?"
            );

            if (result == MessageBoxResult.Yes)
            {
                // Начинаем печать
            }

            // ============================================
            // 6. ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ
            // ============================================
            var deleteResult = CustomMessageBox.ShowConfirmation(
                "Удаление проекта",
                "Вы действительно хотите удалить проект \"TestProject.cli\"?\n\nЭто действие нельзя отменить."
            );

            if (deleteResult == MessageBoxResult.Yes)
            {
                // Удаляем проект
            }

            // ============================================
            // 7. КАСТОМНЫЙ MESSAGEBOX С 3 КНОПКАМИ
            // ============================================
            var saveResult = CustomMessageBox.Show(
                "Несохранённые изменения",
                "У вас есть несохранённые изменения в настройках.\n\nСохранить перед выходом?",
                MessageBoxType.YesNoCancel,
                MessageBoxIcon.Question
            );

            switch (saveResult)
            {
                case MessageBoxResult.Yes:
                    // Сохраняем и выходим
                    break;
                case MessageBoxResult.No:
                    // Выходим без сохранения
                    break;
                case MessageBoxResult.Cancel:
                    // Отменяем выход
                    break;
            }

            // ============================================
            // 8. ДЛИННОЕ СООБЩЕНИЕ С ПРОКРУТКОЙ
            // ============================================
            CustomMessageBox.ShowInformation(
                "Информация о системе",
                @"PrintMate.Terminal v1.0.0

Характеристики:
• Версия .NET: 9.0
• Разрешение: 1024x768
• Архитектура: MVVM + Prism
• База данных: SQLite + EF Core
• Связь с оборудованием: OPC UA

Поддерживаемое оборудование:
• ATM16 (16 лазеров)
• ATM32 (32 лазера)
• Hans GMC сканеры
• Лазерные системы IPG/nLIGHT

Поддерживаемые форматы:
• CLI (Binary)
• CNC (G-code)

© 2025 Все права защищены"
            );

            // ============================================
            // 9. ИСПОЛЬЗОВАНИЕ В TRY-CATCH
            // ============================================
            try
            {
                // Какая-то операция
                throw new System.Exception("Неожиданная ошибка");
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError(
                    "Ошибка",
                    $"Произошла ошибка при выполнении операции:\n\n{ex.Message}"
                );
            }

            // ============================================
            // 10. ВОПРОС ПЕРЕД ОПАСНОЙ ОПЕРАЦИЕЙ
            // ============================================
            var resetResult = CustomMessageBox.Show(
                "Сброс настроек",
                "Вы собираетесь сбросить все настройки к заводским значениям.\n\n" +
                "Все пользовательские конфигурации будут удалены.\n\n" +
                "Продолжить?",
                MessageBoxType.YesNo,
                MessageBoxIcon.Warning
            );

            if (resetResult == MessageBoxResult.Yes)
            {
                // Сбрасываем настройки
            }
        }

        // ============================================
        // ПРИМЕР ИСПОЛЬЗОВАНИЯ В РЕАЛЬНОМ КОДЕ
        // ============================================

        public void DeleteProjectExample(string projectName)
        {
            var result = CustomMessageBox.ShowQuestion(
                "Удаление проекта",
                $"Вы действительно хотите удалить проект \"{projectName}\"?\n\nЭто действие нельзя отменить."
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем проект
                    // await _projectRepository.DeleteAsync(projectId);

                    CustomMessageBox.ShowSuccess(
                        "Успешно",
                        "Проект успешно удалён."
                    );
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.ShowError(
                        "Ошибка удаления",
                        $"Не удалось удалить проект:\n\n{ex.Message}"
                    );
                }
            }
        }

        public void StartPrintWithConfirmation()
        {
            var result = CustomMessageBox.ShowQuestion(
                "Начать печать?",
                "Все системы готовы к печати.\n\nНачать процесс печати?"
            );

            if (result == MessageBoxResult.Yes)
            {
                // Запускаем печать
                CustomMessageBox.ShowInformation(
                    "Печать начата",
                    "Процесс печати запущен. Ожидаемое время: 3:45:00"
                );
            }
        }

        public bool CheckUnsavedChanges()
        {
            var result = CustomMessageBox.Show(
                "Несохранённые изменения",
                "У вас есть несохранённые изменения. Сохранить?",
                MessageBoxType.YesNoCancel,
                MessageBoxIcon.Warning
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // Сохраняем
                    return true;

                case MessageBoxResult.No:
                    // Не сохраняем, но продолжаем
                    return true;

                case MessageBoxResult.Cancel:
                    // Отменяем операцию
                    return false;

                default:
                    return false;
            }
        }
    }
}
