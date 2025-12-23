using Opc2Lib;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.ViewModels
{
    public class CommandInfoViewModel : BindableBase
    {
        private string _command = string.Empty;
        private string _russianName = string.Empty;
        private string _englishName = string.Empty;
        private ValueCommandType _valueCommandType = ValueCommandType.Bool;
        private string _address = string.Empty;
        private CommandType _groupId;

        public string Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        public string RussianName
        {
            get => _russianName;
            set => SetProperty(ref _russianName, value);
        }

        public string EnglishName
        {
            get => _englishName;
            set => SetProperty(ref _englishName, value);
        }

        public ValueCommandType ValueCommandType
        {
            get => _valueCommandType;
            set => SetProperty(ref _valueCommandType, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public CommandType GroupId
        {
            get => _groupId;
            set => SetProperty(ref _groupId, value);
        }

        public string Title => RussianName;

        public CommandInfoViewModel()
        {
            
        }

        public CommandInfoViewModel(CommandInfo commandInfo)
        {
            Command = commandInfo.Command;
            RussianName = commandInfo.RussianName;
            ValueCommandType = commandInfo.ValueCommandType;
            Address = commandInfo.Address;
            GroupId = commandInfo.GroupId;
        }
    }
}
