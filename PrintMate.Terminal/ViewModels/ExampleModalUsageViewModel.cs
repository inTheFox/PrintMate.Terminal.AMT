using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintMate.Terminal.ViewModels
{
    /// <summary>
    /// Пример использования ModalService для асинхронных модальных окон
    /// </summary>
    public class ExampleModalUsageViewModel : BindableBase
    {
        private readonly ModalService _modalService;
        private string _resultText;

        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        public ICommand ShowSimpleModalCommand { get; }
        public ICommand ShowModalWithParametersCommand { get; }
        public ICommand ShowMultipleModalsCommand { get; }
        public ICommand ShowModalWithoutOverlayCommand { get; }

        public ExampleModalUsageViewModel(ModalService modalService)
        {
            _modalService = modalService;

            ShowSimpleModalCommand = new DelegateCommand(async () => await ShowSimpleModal());
            ShowModalWithParametersCommand = new DelegateCommand(async () => await ShowModalWithParameters());
            ShowMultipleModalsCommand = new DelegateCommand(async () => await ShowMultipleModals());
            ShowModalWithoutOverlayCommand = new DelegateCommand(async () => await ShowModalWithoutOverlay());
        }

        /// <summary>
        /// Пример 1: Простое открытие модального окна
        /// </summary>
        private async Task ShowSimpleModal()
        {
            ResultText = "Открываем модальное окно...";

            // Открываем модалку асинхронно - UI поток НЕ блокируется!
            var result = await _modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>(
                modalId: null  // автогенерация ID
            );

            // Этот код выполнится только после закрытия модалки
            if (result.IsSuccess)
            {
                ResultText = $"Модалка закрыта успешно! ViewModel: {result.Result?.GetType().Name}";
            }
            else
            {
                ResultText = "Модалка отменена пользователем";
            }
        }

        /// <summary>
        /// Пример 2: Открытие модального окна с передачей параметров
        /// </summary>
        private async Task ShowModalWithParameters()
        {
            ResultText = "Открываем модалку с параметрами...";

            // Передаём параметры в ViewModel через Dictionary
            var options = new Dictionary<string, object>
            {
                { "Title", "Заголовок из кода" },
                { "Username", "Предзаполненное имя" },
                { "IsEditMode", true }
            };

            var result = await _modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>(
                modalId: null, // автоматическая генерация ID
                options: options
            );

            if (result.IsSuccess)
            {
                // Можем получить данные из ViewModel
                var vm = result.Result;
                ResultText = $"Успешно! ViewModel получил параметры";
            }
            else
            {
                ResultText = "Отменено";
            }
        }

        /// <summary>
        /// Пример 3: Открытие нескольких модалок последовательно
        /// </summary>
        private async Task ShowMultipleModals()
        {
            ResultText = "Открываем первую модалку...";

            // Открываем первую модалку
            var result1 = await _modalService.ShowAsync<SelectFolderView, SelectFolderViewModel>(
                modalId: null
            );

            if (!result1.IsSuccess)
            {
                ResultText = "Первая модалка отменена";
                return;
            }

            ResultText = "Первая модалка закрыта, открываем вторую...";

            // Открываем вторую модалку
            var result2 = await _modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>(
                modalId: null
            );

            if (result2.IsSuccess)
            {
                ResultText = "Обе модалки закрыты успешно!";
            }
            else
            {
                ResultText = "Вторая модалка отменена";
            }
        }

        /// <summary>
        /// Пример 4: Модалка без затемнённого фона
        /// </summary>
        private async Task ShowModalWithoutOverlay()
        {
            ResultText = "Открываем модалку без overlay...";

            // showOverlay: false - не показываем затемнённый фон
            // closeOnBackgroundClick: false - нельзя закрыть кликом по фону
            var result = await _modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>(
                modalId: null,
                options: null,
                showOverlay: false,
                closeOnBackgroundClick: false
            );

            ResultText = result.IsSuccess ? "Закрыто успешно!" : "Отменено";
        }

        /// <summary>
        /// Пример 5: Программное закрытие модального окна из другого места
        /// </summary>
        public async Task CloseModalProgrammatically()
        {
            // Можно закрыть последнее модальное окно программно
            await _modalService.CloseAsync();

            // Или закрыть конкретное окно по ID (если сохранили ID при открытии)
            // await _modalService.CloseAsync(modalId, isSuccess: true);
        }
    }
}
