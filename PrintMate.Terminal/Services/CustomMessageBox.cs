using PrintMate.Terminal.Models;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// Утилита для показа красивых MessageBox через асинхронный ModalService.
    /// Поддерживает как асинхронный API (рекомендуется), так и синхронный для обратной совместимости.
    /// </summary>
    public static class CustomMessageBox
    {
        private static ModalService ModalServiceInstance => ModalService.Instance;

        #region Асинхронные методы (рекомендуется использовать)

        /// <summary>
        /// Показать информационное сообщение с кнопкой OK (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowInformationAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Показать предупреждение с кнопкой OK (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowWarningAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Показать ошибку с кнопкой OK (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowErrorAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Показать сообщение об успехе с кнопкой OK (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowSuccessAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.OK, MessageBoxIcon.Success);
        }

        /// <summary>
        /// Показать вопрос с кнопками Да/Нет (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowQuestionAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.YesNo, MessageBoxIcon.Question);
        }

        /// <summary>
        /// Показать подтверждение с кнопками Да/Нет (асинхронно)
        /// </summary>
        public static Task<Models.MessageBoxResult> ShowConfirmationAsync(string title, string message)
        {
            return ShowAsync(title, message, MessageBoxType.YesNo, MessageBoxIcon.Question);
        }

        /// <summary>
        /// Показать чек-лист подготовки к работе (асинхронно)
        /// </summary>
        public static async Task<bool> ShowPreparationChecklistAsync()
        {
            if (ModalServiceInstance == null)
            {
                throw new System.InvalidOperationException(
                    "ModalService.Instance = null. " +
                    "Убедитесь, что ModalService зарегистрирован в DI и создан до вызова CustomMessageBox."
                );
            }

            ModalResult<ViewModels.ModalsViewModels.PreparationChecklistViewModel> modalResult;

            try
            {
                modalResult = await ModalServiceInstance.ShowAsync<Views.Modals.PreparationChecklistView, ViewModels.ModalsViewModels.PreparationChecklistViewModel>(
                    modalId: null,
                    options: null,
                    showOverlay: true,
                    closeOnBackgroundClick: false
                );
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException(
                    $"Ошибка при вызове ModalService.ShowAsync: {ex.Message}",
                    ex
                );
            }

            if (modalResult != null && modalResult.IsSuccess && modalResult.Result != null)
            {
                return modalResult.Result.AllChecked;
            }

            return false;
        }

        /// <summary>
        /// Показать MessageBox с произвольными параметрами (асинхронно)
        /// </summary>
        public static async Task<Models.MessageBoxResult> ShowAsync(
            string title,
            string message,
            MessageBoxType type = MessageBoxType.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            if (ModalServiceInstance == null)
            {
                throw new System.InvalidOperationException(
                    "ModalService.Instance = null. " +
                    "Убедитесь, что ModalService зарегистрирован в DI и создан до вызова CustomMessageBox."
                );
            }

            var parameters = new Dictionary<string, object>
            {
                { "title", title },
                { "message", message },
                { "type", type },
                { "icon", icon }
            };

            ModalResult<MessageBoxViewModel> modalResult;

            try
            {
                modalResult = await ModalServiceInstance.ShowAsync<MessageBoxView, MessageBoxViewModel>(
                    modalId: null,  // автогенерация ID
                    options: parameters,
                    showOverlay: true,
                    closeOnBackgroundClick: false  // MessageBox нельзя закрыть кликом по фону
                );
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException(
                    $"Ошибка при вызове ModalService.ShowAsync: {ex.Message}",
                    ex
                );
            }

            if (modalResult != null && modalResult.IsSuccess && modalResult.Result != null)
            {
                return modalResult.Result.Result;
            }

            return Models.MessageBoxResult.None;
        }

        #endregion

        #region Синхронные методы (для обратной совместимости, использовать не рекомендуется)

        /// <summary>
        /// Показать информационное сообщение с кнопкой OK (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowInformationAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowInformation(string title, string message)
        {
            return ShowInformationAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать предупреждение с кнопкой OK (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowWarningAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowWarning(string title, string message)
        {
            return ShowWarningAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать ошибку с кнопкой OK (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowErrorAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowError(string title, string message)
        {
            return ShowErrorAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать сообщение об успехе с кнопкой OK (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowSuccessAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowSuccess(string title, string message)
        {
            return ShowSuccessAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать вопрос с кнопками Да/Нет (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowQuestionAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowQuestion(string title, string message)
        {
            return ShowQuestionAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать подтверждение с кнопками Да/Нет (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowConfirmationAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult ShowConfirmation(string title, string message)
        {
            return ShowConfirmationAsync(title, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Показать MessageBox с произвольными параметрами (синхронно, блокирует UI)
        /// Рекомендуется использовать ShowAsync вместо этого метода.
        /// </summary>
        public static Models.MessageBoxResult Show(
            string title,
            string message,
            MessageBoxType type = MessageBoxType.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return ShowAsync(title, message, type, icon).GetAwaiter().GetResult();
        }

        #endregion
    }
}
