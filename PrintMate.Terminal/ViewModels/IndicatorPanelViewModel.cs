using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Tools.Command;
using Opc2Lib;
using PrintMate.Terminal.Views;

namespace PrintMate.Terminal.ViewModels
{
    public class IndicatorPanelViewModel : BindableBase
    {
        private CommandInfo _commandInfo;
        public CommandInfo CommandInfo
        {
            get => _commandInfo;
            set => SetProperty(ref _commandInfo, value);
        }

        public IndicatorPanelViewModel()
        {
        }

        public void OnLoaded(object b)
        {
            CommandInfo = (b as IndicatorPanel).CommandInfo;
            Console.WriteLine($"{CommandInfo.Title}");
        }
    }
}
