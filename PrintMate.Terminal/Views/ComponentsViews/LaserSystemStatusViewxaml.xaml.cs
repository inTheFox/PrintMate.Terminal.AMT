using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LaserLib.Models;

namespace PrintMate.Terminal.Views.ComponentsViews
{
    /// <summary>
    /// Логика взаимодействия для LaserSystemStatusViewxaml.xaml
    /// </summary>
    public partial class LaserSystemStatusViewxaml : UserControl
    {
        public static readonly DependencyProperty LaserStatusProperty =
            DependencyProperty.Register(
                nameof(LaserStatus),
                typeof(LaserStatus),
                typeof(LaserSystemStatusViewxaml),
                new PropertyMetadata(new LaserStatus()));
        public LaserStatus LaserStatus
        {
            get => (LaserStatus)GetValue(LaserStatusProperty);
            set => SetValue(LaserStatusProperty, value);
        }




        public static readonly DependencyProperty StatusDescriptionsProperty =
            DependencyProperty.Register(
                nameof(StatusDescriptions),
                typeof(Dictionary<int, string>),
                typeof(LaserSystemStatusViewxaml),
                new PropertyMetadata(new Dictionary<int, string>()));

        public Dictionary<int, string> StatusDescriptions
        {
            get => (Dictionary<int, string>)GetValue(StatusDescriptionsProperty);
            set => SetValue(StatusDescriptionsProperty, value);
        }

        public LaserSystemStatusViewxaml()
        {
            StatusDescriptions = new Dictionary<int, string>
            {
                { 0, "Перепол. буф. ком." },
                { 1, "Перегрев" },
                { 2, "Излучение ВКЛ" },
                { 3, "Обратное отражение" },
                { 4, "Аналог.управление" },
                { 5, "Резерв" },
                { 6, "Резерв" },
                { 7, "Резерв" },
                { 8, "Пилот. лазер включен" },
                { 9, "Длит. импульса мала" },
                { 10, "Непрерывный режим (CW)" },
                { 11, "ИП (ВЫКЛ/ВКЛ)" },
                { 12, "Модуляция" },
                { 13, "Резерв (Compatibility Mode)" },
                { 14, "Резерв" },
                { 15, "Резерв" },
                { 16, "Режим Gate" },
                { 17, "Резерв" },
                { 18, "HW управл. излучением" },
                { 19, "Неисправность ИП" },
                { 20, "Резерв" },
                { 21, "Резерв" },
                { 22, "Резерв" },
                { 23, "Резерв" },
                { 24, "Низкая температура" },
                { 25, "Ошибка ИП" },
                { 26, "Резерв" },
                { 27, "HW управл. пилот. лазером" },
                { 28, "Предупр. пилот. лазера" },
                { 29, "Критическая ошибка" },
                { 30, "Обрыв волокна" },
                { 31, "Резерв" }
            };

            InitializeComponent();
        }
    }
}
