using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Tools.Command;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class ProjectLoadingModalViewModel : BindableBase
    {
        public string ProjectName { get; set; }
        public RelayCommand NextBackground { get; set; }

        public ProjectLoadingModalViewModel()
        {
            
        }
    }
}
