using System.Collections.Generic;
using Hans.NET;
using PrintMate.Terminal.Parsers.Shared;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;

namespace ProjectParserTest.Parsers.Shared.Models
{
    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point()
        {
            
        }

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public enum BlockType
    {
        PolyLine,
        Hatch
    }


    public class Region
    {
        public int Id { get; set; }
        public int LaserNum { get; set; }
        public GeometryRegion GeometryRegion { get; set; }
        public RegionParameters Parameters { get; set; }
        public Part Part { get; set; }
        public double ExposeLength { get; set; }
        public List<PolyLine> PolyLines { get; set; }
        public BlockType Type { get; set; } = BlockType.Hatch;

        public Region()
        {
            PolyLines = new List<PolyLine>();
        }
    }
}
