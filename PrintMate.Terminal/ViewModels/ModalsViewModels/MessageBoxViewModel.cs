using HandyControl.Tools.Command;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using Prism.Mvvm;
using System.Windows;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class MessageBoxViewModel : BindableBase
    {
        private string _title = "Сообщение";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private MessageBoxType _type = MessageBoxType.OK;
        public MessageBoxType Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
                UpdateButtonVisibility();
            }
        }

        private MessageBoxIcon _icon = MessageBoxIcon.None;
        public MessageBoxIcon Icon
        {
            get => _icon;
            set
            {
                SetProperty(ref _icon, value);
                UpdateIconProperties();
            }
        }

        public Models.MessageBoxResult Result { get; set; } = Models.MessageBoxResult.None;

        // Видимость кнопок
        private Visibility _okButtonVisibility = Visibility.Collapsed;
        public Visibility OkButtonVisibility
        {
            get => _okButtonVisibility;
            set => SetProperty(ref _okButtonVisibility, value);
        }

        private Visibility _yesButtonVisibility = Visibility.Collapsed;
        public Visibility YesButtonVisibility
        {
            get => _yesButtonVisibility;
            set => SetProperty(ref _yesButtonVisibility, value);
        }

        private Visibility _noButtonVisibility = Visibility.Collapsed;
        public Visibility NoButtonVisibility
        {
            get => _noButtonVisibility;
            set => SetProperty(ref _noButtonVisibility, value);
        }

        private Visibility _cancelButtonVisibility = Visibility.Collapsed;
        public Visibility CancelButtonVisibility
        {
            get => _cancelButtonVisibility;
            set => SetProperty(ref _cancelButtonVisibility, value);
        }

        // Свойства иконки
        private string _iconText = "";
        public string IconText
        {
            get => _iconText;
            set => SetProperty(ref _iconText, value);
        }

        private string _iconColor = "#FFFFFF";
        public string IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        private string _iconBackgroundColor = "#2196F3";
        public string IconBackgroundColor
        {
            get => _iconBackgroundColor;
            set => SetProperty(ref _iconBackgroundColor, value);
        }

        private Visibility _iconVisibility = Visibility.Collapsed;
        public Visibility IconVisibility
        {
            get => _iconVisibility;
            set => SetProperty(ref _iconVisibility, value);
        }

        // Команды
        public RelayCommand OkCommand { get; set; }
        public RelayCommand YesCommand { get; set; }
        public RelayCommand NoCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public MessageBoxViewModel()
        {
            OkCommand = new RelayCommand(_ => OnOk());
            YesCommand = new RelayCommand(_ => OnYes());
            NoCommand = new RelayCommand(_ => OnNo());
            CancelCommand = new RelayCommand(_ => OnCancel());
        }

        public void Initialize(string title, string message, MessageBoxType type, MessageBoxIcon icon)
        {
            Title = title;
            Message = message;
            Icon = icon;
            Type = type;
        }

        private void UpdateButtonVisibility()
        {
            // Скрываем все кнопки
            OkButtonVisibility = Visibility.Collapsed;
            YesButtonVisibility = Visibility.Collapsed;
            NoButtonVisibility = Visibility.Collapsed;
            CancelButtonVisibility = Visibility.Collapsed;

            // Показываем нужные кнопки
            switch (Type)
            {
                case MessageBoxType.OK:
                    OkButtonVisibility = Visibility.Visible;
                    break;

                case MessageBoxType.YesNo:
                    YesButtonVisibility = Visibility.Visible;
                    NoButtonVisibility = Visibility.Visible;
                    break;

                case MessageBoxType.OKCancel:
                    OkButtonVisibility = Visibility.Visible;
                    CancelButtonVisibility = Visibility.Visible;
                    break;

                case MessageBoxType.YesNoCancel:
                    YesButtonVisibility = Visibility.Visible;
                    NoButtonVisibility = Visibility.Visible;
                    CancelButtonVisibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateIconProperties()
        {
            switch (Icon)
            {
                case MessageBoxIcon.None:
                    IconVisibility = Visibility.Collapsed;
                    break;

                case MessageBoxIcon.Information:
                    IconVisibility = Visibility.Visible;
                    IconText = "i";
                    IconColor = "#FFFFFF";
                    IconBackgroundColor = "#2196F3"; // Синий
                    break;

                case MessageBoxIcon.Warning:
                    IconVisibility = Visibility.Visible;
                    IconText = "!";
                    IconColor = "#FFFFFF";
                    IconBackgroundColor = "#FF9800"; // Оранжевый
                    break;

                case MessageBoxIcon.Error:
                    IconVisibility = Visibility.Visible;
                    IconText = "×";
                    IconColor = "#FFFFFF";
                    IconBackgroundColor = "#F44336"; // Красный
                    break;

                case MessageBoxIcon.Question:
                    IconVisibility = Visibility.Visible;
                    IconText = "?";
                    IconColor = "#FFFFFF";
                    IconBackgroundColor = "#9C27B0"; // Фиолетовый
                    break;

                case MessageBoxIcon.Success:
                    IconVisibility = Visibility.Visible;
                    IconText = "✓";
                    IconColor = "#FFFFFF";
                    IconBackgroundColor = "#4CAF50"; // Зелёный
                    break;
            }
        }

        private void OnOk()
        {
            Result = Models.MessageBoxResult.OK;
            CloseModal();
        }

        private void OnYes()
        {
            Result = Models.MessageBoxResult.Yes;
            CloseModal();
        }

        private void OnNo()
        {
            Result = Models.MessageBoxResult.No;
            CloseModal();
        }

        private void OnCancel()
        {
            Result = Models.MessageBoxResult.Cancel;
            CloseModal();
        }

        private void CloseModal()
        {
            ModalService.Instance.Close();
        }
    }
}
