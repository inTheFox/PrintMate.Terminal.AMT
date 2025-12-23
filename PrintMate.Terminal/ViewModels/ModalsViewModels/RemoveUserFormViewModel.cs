using PrintMate.Terminal.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class RemoveUserFormViewModel : BindableBase, IViewModelForm
    {
        public RemoveUserFormViewModel(UserService userService)
        {
            this.userService = userService;
        }

        private readonly UserService userService;
        private RelayCommand _deleteCommand;
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsDeleted = false;

        public RelayCommand CloseCommand { get; set; }

        public RelayCommand DeleteCommand
        {
            get => _deleteCommand ??= new RelayCommand(async obj =>
            {
                await Delete();
            });
        }

        private async Task Delete()
        {
            IsDeleted = true;
            await userService.Remove(Name);
            CloseCommand.Execute(null);
        }
    }
}
