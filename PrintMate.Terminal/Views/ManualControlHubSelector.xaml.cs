using Prism.Ioc;
using Prism.Regions;
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

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для ManualControlHubSelector.xaml
    /// </summary>
    public partial class ManualControlHubSelector : UserControl
    {
        public static readonly DependencyProperty RegionNameProperty =
            DependencyProperty.Register(
                nameof(RegionName),
                typeof(string),
                typeof(ManualControlHubSelector),
                new PropertyMetadata("UNSET"));

        public string RegionName
        {
            get => (string)GetValue(RegionNameProperty);
            set => SetValue(RegionNameProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ManualControlHubSelector),
                new PropertyMetadata("UNSET"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ImageSrcProperty =
            DependencyProperty.Register(
                nameof(ImageSrc),
                typeof(string),
                typeof(ManualControlHubSelector),
                new PropertyMetadata("UNSET"));

        public string ImageSrc
        {
            get => (string)GetValue(ImageSrcProperty);
            set => SetValue(ImageSrcProperty, value);
        }


        private readonly IRegionManager regionManager;

        public ManualControlHubSelector()
        {
            this.regionManager = Bootstrapper.ContainerProvider.Resolve<IRegionManager>();
            InitializeComponent();
        }

        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void UIElement_OnTouchUp(object sender, TouchEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            regionManager.RequestNavigate(Bootstrapper.ManualContent, RegionName);
        }

        private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
