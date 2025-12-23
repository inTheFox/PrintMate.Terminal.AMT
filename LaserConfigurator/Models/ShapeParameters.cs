namespace LaserConfigurator.Models
{
    /// <summary>
    /// Параметры фигуры для рисования
    /// </summary>
    public class ShapeParameters
    {
        /// <summary>
        /// Тип фигуры
        /// </summary>
        public ShapeType Type { get; set; }

        /// <summary>
        /// X координата центра (мм)
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y координата центра (мм)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Размер фигуры (мм)
        /// Для квадрата - длина стороны
        /// Для круга - диаметр
        /// Для треугольника - длина стороны равностороннего треугольника
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// Скорость маркировки (мм/с)
        /// </summary>
        public double Speed { get; set; } = 1000;

        /// <summary>
        /// Мощность лазера (Вт)
        /// </summary>
        public double Power { get; set; } = 100;

        /// <summary>
        /// Диаметр пучка лазера (микроны)
        /// </summary>
        public double BeamDiameter { get; set; } = 80;

        /// <summary>
        /// Разделить фигуру между двумя лазерами
        /// </summary>
        public bool SplitBetweenLasers { get; set; } = false;
    }
}
