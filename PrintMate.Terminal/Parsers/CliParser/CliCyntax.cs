using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Parsers.CliParser
{
    public class CliSyntax
    {
        public const short GEOMETRY_LAYER_LONG_BINARY_TAG = 127;
        public const short GEOMETRY_LAYER_SHORT_BINARY_TAG = 128;
        public const short GEOMETRY_POLYLINE_SHORT_BINARY_TAG = 129;
        public const short GEOMETRY_POLYLINE_LONG_BINARY_TAG = 130;
        public const short GEOMETRY_HATCHES_SHORT_BINARY_TAG = 131;
        public const short GEOMETRY_HATCHES_LONG_BINARY_TAG = 132;

        // Кастомные форматы PrintMate
        public const short GEOMETRY_POLYLINE_INT_BINARY_TAG = 230;
        public const short GEOMETRY_HATCHES_INT_BINARY_TAG = 232;
    }
}
