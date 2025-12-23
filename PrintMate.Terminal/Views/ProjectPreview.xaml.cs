using SharpDX;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для ProjectPreview.xaml (3D визуализация)
    /// </summary>
    public partial class ProjectPreview : UserControl
    {
        private double _minZoomDistance = 0.1;
        private double _maxZoomDistance = 10000;
        private PerspectiveCamera _camera;
        public ProjectPreview()
        {
            InitializeComponent();

            _camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 10, 0),
                LookDirection = new Vector3D(0, -1, 0),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60,
                NearPlaneDistance = _minZoomDistance,
                FarPlaneDistance = _maxZoomDistance,
                CreateLeftHandSystem = true
            };
            Viewport.Camera = _camera;
        }
    }
}
