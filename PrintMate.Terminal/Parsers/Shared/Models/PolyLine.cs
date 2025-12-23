using System.Collections.Generic;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Parsers.Shared.Models;

public class PolyLine
{
    public List<Point> Points { get; set; }

    public PolyLine()
    {
        
    }

    public PolyLine(List<Point> points)
    {
        Points = points;
    }

    public void Add(Point point)
    {
        if (Points == null)
        {
            Points = new List<Point>();
        }
        Points.Add(point);
    }
}