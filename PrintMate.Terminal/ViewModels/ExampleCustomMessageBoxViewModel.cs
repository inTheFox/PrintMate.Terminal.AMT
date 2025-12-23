using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintMate.Terminal.ViewModels
{
    /// <summary>
    /// Демонстрация использования обновлённого CustomMessageBox с ModalService
    /// </summary>
    public class ExampleCustomMessageBoxViewModel : BindableBase
    {
        private string _statusText = "Нажмите кнопку чтобы показать MessageBox";

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ICommand ShowInformationCommand { get; }
        public ICommand ShowWarningCommand { get; }
        public ICommand ShowErrorCommand { get; }
        public ICommand ShowSuccessCommand { get; }
        public ICommand ShowQuestionCommand { get; }
        public ICommand ShowConfirmationCommand { get; }
        public ICommand ShowCustomCommand { get; }
        public ICommand DemoWorkflowCommand { get; }

        public ExampleCustomMessageBoxViewModel()
        {
            ShowInformationCommand = new DelegateCommand(async () => await ShowInformation());
            ShowWarningCommand = new DelegateCommand(async () => await ShowWarning());
            ShowErrorCommand = new DelegateCommand(async () => await ShowError());
            ShowSuccessCommand = new DelegateCommand(async () => await ShowSuccess());
            ShowQuestionCommand = new DelegateCommand(async () => await ShowQuestion());
            ShowConfirmationCommand = new DelegateCommand(async () => await ShowConfirmation());
            ShowCustomCommand = new DelegateCommand(async () => await ShowCustom());
            DemoWorkflowCommand = new DelegateCommand(async () => await DemoWorkflow());
        }

        /// <summary>
        /// Пример 1: Информационное сообщение
        /// </summary>
        private async Task ShowInformation()
        {
            StatusText = "Показываем информационное сообщение...";

            await CustomMessageBox.ShowInformationAsync(
                "Информация",
                "Это пример информационного сообщения.\n\nUI поток НЕ блокируется!"
            );

            StatusText = "Информационное сообщение закрыто";
        }

        /// <summary>
        /// Пример 2: Предупреждение
        /// </summary>
        private async Task ShowWarning()
        {
            StatusText = "Показываем предупреждение...";

            await CustomMessageBox.ShowWarningAsync(
                "Внимание",
                "Это предупреждающее сообщение.\n\nОбратите внимание на оранжевую иконку."
            );

            StatusText = "Предупреждение закрыто";
        }

        /// <summary>
        /// Пример 3: Ошибка
        /// </summary>
        private async Task ShowError()
        {
            StatusText = "Показываем ошибку...";

            await CustomMessageBox.ShowErrorAsync(
                "Ошибка",
                "Произошла критическая ошибка!\n\nКрасная иконка обозначает серьёзность."
            );

            StatusText = "Сообщение об ошибке закрыто";
        }

        /// <summary>
        /// Пример 4: Успешное завершение
        /// </summary>
        private async Task ShowSuccess()
        {
            StatusText = "Показываем сообщение об успехе...";

            await CustomMessageBox.ShowSuccessAsync(
                "Готово!",
                "Операция выполнена успешно.\n\nЗелёная галочка показывает успех."
            );

            StatusText = "Сообщение об успехе закрыто";
        }

        /// <summary>
        /// Пример 5: Вопрос с выбором Да/Нет
        /// </summary>
        private async Task ShowQuestion()
        {
            StatusText = "Показываем вопрос...";

            var result = await CustomMessageBox.ShowQuestionAsync(
                "Подтверждение",
                "Вы хотите продолжить операцию?"
            );

            StatusText = result == MessageBoxResult.Yes
                ? "Пользователь выбрал ДА"
                : "Пользователь выбрал НЕТ";
        }

        /// <summary>
        /// Пример 6: Подтверждение действия
        /// </summary>
        private async Task ShowConfirmation()
        {
            StatusText = "Показываем подтверждение...";

            var result = await CustomMessageBox.ShowConfirmationAsync(
                "Удалить файл?",
                "Это действие нельзя отменить."
            );

            if (result == MessageBoxResult.Yes)
            {
                StatusText = "Файл будет удалён (имитация)";
                await Task.Delay(1000);
                await CustomMessageBox.ShowSuccessAsync("Готово", "Файл успешно удалён");
                StatusText = "Файл удалён";
            }
            else
            {
                StatusText = "Удаление отменено пользователем";
            }
        }

        /// <summary>
        /// Пример 7: Кастомный MessageBox с произвольными параметрами
        /// </summary>
        private async Task ShowCustom()
        {
            StatusText = "Показываем кастомный MessageBox...";

            var result = await CustomMessageBox.ShowAsync(
                title: "Сохранить изменения?",
                message: "Вы внесли изменения в документ.\n\nЧто вы хотите сделать?",
                type: MessageBoxType.YesNoCancel,
                icon: MessageBoxIcon.Question
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    StatusText = "Сохраняем изменения...";
                    await Task.Delay(1000);
                    await CustomMessageBox.ShowSuccessAsync("Сохранено", "Изменения сохранены");
                    StatusText = "Изменения сохранены";
                    break;

                case MessageBoxResult.No:
                    StatusText = "Изменения отменены без сохранения";
                    break;

                case MessageBoxResult.Cancel:
                    StatusText = "Операция отменена пользователем";
                    break;

                default:
                    StatusText = "Неизвестный результат";
                    break;
            }
        }

        /// <summary>
        /// Пример 8: Демонстрация полного workflow с несколькими MessageBox
        /// </summary>
        private async Task DemoWorkflow()
        {
            StatusText = "Запуск демо workflow...";

            // Шаг 1: Подтверждение начала операции
            var startResult = await CustomMessageBox.ShowQuestionAsync(
                "Начать процесс?",
                "Будет выполнена демонстрация последовательности операций."
            );

            if (startResult != MessageBoxResult.Yes)
            {
                StatusText = "Процесс отменён пользователем";
                return;
            }

            // Шаг 2: Информируем о начале
            StatusText = "Выполняется шаг 1/3...";
            await Task.Delay(1500);

            await CustomMessageBox.ShowInformationAsync(
                "Шаг 1 выполнен",
                "Первый этап успешно завершён"
            );

            // Шаг 3: Предупреждение
            StatusText = "Выполняется шаг 2/3...";
            await Task.Delay(1500);

            await CustomMessageBox.ShowWarningAsync(
                "Предупреждение",
                "Следующий шаг может занять некоторое время"
            );

            // Шаг 4: Имитация долгой операции
            StatusText = "Выполняется шаг 3/3 (долгая операция)...";
            await Task.Delay(2000);

            // Шаг 5: Симуляция случайного успеха/ошибки
            var random = new System.Random();
            var success = random.Next(0, 2) == 0;

            if (success)
            {
                await CustomMessageBox.ShowSuccessAsync(
                    "Процесс завершён!",
                    "Все операции выполнены успешно."
                );
                StatusText = "Workflow завершён успешно";
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync(
                    "Произошла ошибка",
                    "Во время выполнения операции возникла ошибка.\n\nПопробуйте ещё раз."
                );
                StatusText = "Workflow завершён с ошибкой";
            }
        }
    }
}
