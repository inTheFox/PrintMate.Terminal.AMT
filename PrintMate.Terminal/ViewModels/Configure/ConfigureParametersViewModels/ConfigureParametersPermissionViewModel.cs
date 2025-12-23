using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersPermissionViewModel : BindableBase
    {
        private string _permissionKey;
        public string PermissionKey
        {
            get => _permissionKey;
            set => SetProperty(ref _permissionKey, value);
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        //private string _category;
        //public string Category
        //{
        //    get => _category;
        //    set => SetProperty(ref _category, value);
        //}
    }
}
