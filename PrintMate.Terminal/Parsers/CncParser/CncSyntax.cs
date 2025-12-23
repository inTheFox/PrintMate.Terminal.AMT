using System;
using System.Collections.Generic;

namespace PrintMate.Terminal.Parsers.CncParser
{
    /// <summary>
    /// Константы и G/M коды для CNC парсера
    /// </summary>
    public static class CncSyntax
    {
        /// <summary>
        /// G-коды (команды движения)
        /// </summary>
        public static class GCodes
        {
            /// <summary>G0 - Быстрое перемещение (лазер выключен)</summary>
            public const string RapidMove = "G0";

            /// <summary>G1 - Линейное перемещение (лазер включен)</summary>
            public const string LinearMove = "G1";

            /// <summary>G90 - Абсолютное позиционирование</summary>
            public const string AbsolutePositioning = "G90";

            /// <summary>G91 - Относительное позиционирование</summary>
            public const string RelativePositioning = "G91";
        }

        /// <summary>
        /// M-коды (машинные команды)
        /// </summary>
        public static class MCodes
        {
            /// <summary>M3 - Включить лазер</summary>
            public const string LaserOn = "M3";

            /// <summary>M5 - Выключить лазер</summary>
            public const string LaserOff = "M5";

            /// <summary>M702 - Установить мощность лазера</summary>
            public const string SetLaserPower = "M702";

            /// <summary>M704 - Установить скорость лазера</summary>
            public const string SetLaserSpeed = "M704";
        }

        /// <summary>
        /// Параметры команд
        /// </summary>
        public static class Parameters
        {
            /// <summary>X координата</summary>
            public const char X = 'X';

            /// <summary>Y координата</summary>
            public const char Y = 'Y';

            /// <summary>Z координата (высота слоя)</summary>
            public const char Z = 'Z';

            /// <summary>P - мощность (для M702)</summary>
            public const char P = 'P';

            /// <summary>S - скорость (для M704)</summary>
            public const char S = 'S';
        }

        /// <summary>
        /// Префиксы комментариев в CNC
        /// </summary>
        public static class Comments
        {
            public const string Semicolon = ";";
            public const string Parenthesis = "(";
            public const string ParenthesisEnd = ")";
        }

        /// <summary>
        /// Ключи конфигурации в комментариях
        /// </summary>
        public static class ConfigKeys
        {
            /// <summary>Название материала</summary>
            public const string Material = "MATERIAL";

            /// <summary>Толщина слоя (мм)</summary>
            public const string LayerHeight = "LAYER_HEIGHT";

            /// <summary>Название проекта</summary>
            public const string ProjectName = "PROJECT_NAME";

            /// <summary>Номер слоя</summary>
            public const string LayerNumber = "LAYER";

            /// <summary>Тип региона (CONTOUR, INFILL, SUPPORT и т.д.)</summary>
            public const string RegionType = "REGION_TYPE";

            /// <summary>Диаметр луча (мкм)</summary>
            public const string BeamDiameter = "BEAM_DIAMETER";

            /// <summary>Расстояние между штрихами (мкм)</summary>
            public const string HatchDistance = "HATCH_DISTANCE";

            /// <summary>Угол штриховки (градусы)</summary>
            public const string HatchAngle = "HATCH_ANGLE";
        }

        /// <summary>
        /// Маппинг строковых типов регионов на enum GeometryRegion
        /// </summary>
        public static readonly Dictionary<string, ProjectParserTest.Parsers.Shared.Enums.GeometryRegion> RegionTypeMap = new Dictionary<string, ProjectParserTest.Parsers.Shared.Enums.GeometryRegion>(StringComparer.OrdinalIgnoreCase)
        {
            { "INFILL", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Infill },
            { "CONTOUR", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Contour },
            { "SUPPORT", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Support },
            { "SUPPORT_FILL", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.SupportFill },
            { "UPSKIN", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Upskin },
            { "DOWNSKIN", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Downskin },
            { "CONTOUR_UPSKIN", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.ContourUpskin },
            { "CONTOUR_DOWNSKIN", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.ContourDownskin },
            { "EDGES", ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Edges },
        };
    }
}
