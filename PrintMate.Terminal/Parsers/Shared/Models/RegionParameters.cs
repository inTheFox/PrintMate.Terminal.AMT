using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Parsers.Shared
{
    public class RegionParameters
    {
        /// <summary>Диаметр лазерного луча (мкм)</summary>
        public double LaserBeamDiameter { get; set; }

        /// <summary>Мощность лазера (Вт)</summary>
        public double LaserPower { get; set; }

        /// <summary>Скорость сканирования лазера (мм/с)</summary>
        public double LaserSpeed { get; set; }

        /// <summary>Режим SkyWriting (лазер не выключается между сегментами)</summary>
        public double Skywriting { get; set; }

        /// <summary>Расстояние между штрихами (мкм)</summary>
        public double HatchDistance { get; set; }

        /// <summary>Угол штриховки (градусы)</summary>
        public double Angle { get; set; }

        public override string ToString()
        {
            return $"Power:{LaserPower}W, Speed:{LaserSpeed}mm/s, Beam:{LaserBeamDiameter}μm, SW:{Skywriting}";
        }
    }
}
