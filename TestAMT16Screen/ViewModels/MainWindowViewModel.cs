using System;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using Prism.Mvvm;

namespace TestAMT16Screen.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public RelayCommand TestCommand { get; set; }

        public MainWindowViewModel()
        {
            TestCommand = new RelayCommand((e) =>
            {
                MessageBox.Show("Clicked !!!");
            });
        }
    }
}
