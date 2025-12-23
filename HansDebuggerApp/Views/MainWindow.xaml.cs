using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using HansDebuggerApp.Opc;
using Opc2Lib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HansDebuggerApp.Views
{
    public partial class MainWindow
    {
        public static MainWindow Instance;


        public MainWindow(ILogicControllerProvider provider, ILogicControllerObserver observer)
        {
            Instance = this;
            InitializeComponent();
        }
    }
}
