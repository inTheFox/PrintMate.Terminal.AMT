using HandyControl.Tools.Command;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc2Lib;

namespace PrintMate.Terminal.Models
{
    public class IndicatorItemViewModel : BindableBase
    {
        private string _title;
        private string _format;
        private double _value;
        private ISeries[] _series;
        private Axis[] _xAxes;
        private Axis[] _yAxes;
        private string _command;

        public CommandInfo CommandInfo { get; set; }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        public string Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public ISeries[] Series
        {
            get => _series;
            set => SetProperty(ref _series, value);
        }

        public Axis[] XAxes
        {
            get => _xAxes;
            set => SetProperty(ref _xAxes, value);
        }

        public Axis[] YAxes
        {
            get => _yAxes;
            set => SetProperty(ref _yAxes, value);
        }

        // Храним значения для графика
        public ObservableCollection<double> ChartValues { get; set; } = new ObservableCollection<double>();
        public RelayCommand Callback { get; set; }

        public IndicatorItemViewModel()
        {
            
        }
        public IndicatorItemViewModel(string title, string format, string command)
        {
            Title = title;
            Format = format;
            Command = command;
        }

    }
}
