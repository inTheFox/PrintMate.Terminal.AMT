using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class SelectCncProjectModalViewModel : BindableBase, IViewModelForm
    {
        private string _selectedMode;
        public string SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        public RelayCommand SingleCommand { get; set; }
        public RelayCommand AllCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }

        public SelectCncProjectModalViewModel()
        {
            SingleCommand = new RelayCommand((e) =>
            {
                MessageBox.Show("SINGLE");
                SelectedMode = "single";
                CloseCommand?.Execute(null);
            });
            AllCommand = new RelayCommand((e) =>
            {
                SelectedMode = "all";
                CloseCommand?.Execute(null);
            });
        }

    }
}
