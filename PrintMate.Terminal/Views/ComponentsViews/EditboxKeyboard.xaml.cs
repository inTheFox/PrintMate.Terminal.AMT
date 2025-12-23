using PrintMate.Terminal.Enums;
using PrintMate.Terminal.Services;
using Prism.Ioc;
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

namespace PrintMate.Terminal.Views.ComponentsViews
{
    /// <summary>
    /// Логика взаимодействия для EditboxKeyboard.xaml
    /// </summary>
    public partial class EditboxKeyboard : UserControl
    {
        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(KeyboardType),
                typeof(KeyboardType),
                typeof(EditboxKeyboard),
                new PropertyMetadata(Views.KeyboardType.Full));

        public KeyboardType KeyboardType
        {
            get => (KeyboardType)GetValue(CommandInfoProperty);
            set => SetValue(CommandInfoProperty, value);
        }

        // Исправлено: DependencyProperty должен соответствовать имени свойства
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(EditboxKeyboard),
                new PropertyMetadata(""));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Исправлено: DependencyProperty должен соответствовать имени свойства
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(EditboxKeyboard),
                new PropertyMetadata("Label"));


        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }


        //KeyboardValueType
        public static readonly DependencyProperty ValueTypeProperty =
            DependencyProperty.Register(
                nameof(ValueType),
                typeof(KeyboardValueType),
                typeof(EditboxKeyboard),
                new PropertyMetadata(KeyboardValueType.String));
        public KeyboardValueType ValueType
        {
            get => (KeyboardValueType)GetValue(ValueTypeProperty);
            set => SetValue(ValueTypeProperty, value);
        }

        private readonly KeyboardService _keyboardService;

        public EditboxKeyboard()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _keyboardService = Bootstrapper.ContainerProvider.Resolve<KeyboardService>();
            }
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ValueType == KeyboardValueType.String)
            {
                Value = _keyboardService.Show(KeyboardType.Full, $"{Label}", Value);
            }
            else
            {
                string value = _keyboardService.Show(KeyboardType.Numpad, $"{Label}", Value);

                switch (ValueType)
                {
                    case KeyboardValueType.Int:
                        if (int.TryParse(value, out var intResult))
                        {
                            Value = intResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Uint:
                        if (int.TryParse(value, out var uintResult))
                        {
                            Value = uintResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Short:
                        if (int.TryParse(value, out var shortResult))
                        {
                            Value = shortResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Ushort:
                        if (int.TryParse(value, out var ushortResult))
                        {
                            Value = ushortResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Float:
                        if (int.TryParse(value, out var floatResult))
                        {
                            Value = floatResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Double:
                        if (int.TryParse(value, out var doubleResult))
                        {
                            Value = doubleResult.ToString();
                        }
                        break;
                    case KeyboardValueType.Long:
                        if (int.TryParse(value, out var longResult))
                        {
                            Value = longResult.ToString();
                        }
                        break;
                    case KeyboardValueType.ULong:
                        if (int.TryParse(value, out var ulongResult))
                        {
                            Value = ulongResult.ToString();
                        }
                        break;
                }
            }
        }
    }
}
