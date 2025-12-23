using Prism.Mvvm;
using ProjectParserTest.Parsers.Shared.Enums;

namespace PrintMate.Terminal.ViewModels
{
    /// <summary>
    /// Параметры лазера для определенного типа региона
    /// </summary>
    public class RegionTypeParameters : BindableBase
    {
        private GeometryRegion _regionType;
        private string _displayName;
        private double _laserPower;
        private double _laserSpeed;
        private double _laserBeamDiameter;
        private bool _isEnabled;
        private bool _hasRegions;

        /// <summary>Тип региона</summary>
        public GeometryRegion RegionType
        {
            get => _regionType;
            set => SetProperty(ref _regionType, value);
        }

        /// <summary>Отображаемое имя типа региона</summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>Мощность лазера (Вт)</summary>
        public double LaserPower
        {
            get => _laserPower;
            set => SetProperty(ref _laserPower, value);
        }

        /// <summary>Скорость сканирования (мм/с)</summary>
        public double LaserSpeed
        {
            get => _laserSpeed;
            set => SetProperty(ref _laserSpeed, value);
        }

        /// <summary>Диаметр лазерного пучка (мкм)</summary>
        public double LaserBeamDiameter
        {
            get => _laserBeamDiameter;
            set => SetProperty(ref _laserBeamDiameter, value);
        }

        /// <summary>Включен ли этот тип региона для редактирования</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>Есть ли регионы этого типа в детали</summary>
        public bool HasRegions
        {
            get => _hasRegions;
            set => SetProperty(ref _hasRegions, value);
        }

        /// <summary>
        /// Возвращает русское название типа региона
        /// </summary>
        public static string GetDisplayName(GeometryRegion regionType)
        {
            return regionType switch
            {
                GeometryRegion.Infill => "Заполнение (Infill)",
                GeometryRegion.SupportFill => "Заполнение поддержки",
                GeometryRegion.Support => "Контур поддержки",
                GeometryRegion.Contour => "Контур детали",
                GeometryRegion.ContourUpskin => "Контур верхней поверхности",
                GeometryRegion.ContourDownskin => "Контур нижней поверхности",
                GeometryRegion.Upskin => "Верхняя поверхность (Upskin)",
                GeometryRegion.Downskin => "Нижняя поверхность (Downskin)",
                GeometryRegion.Edges => "Края детали",
                _ => regionType.ToString()
            };
        }
    }
}
