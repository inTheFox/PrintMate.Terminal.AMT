using System.Collections.Generic;
using System.Linq;
using ProjectParserTest.Parsers.CliParser;

namespace ProjectParserTest.Parsers.Shared.Models
{
    public class Layer
    {
        public int Id { get; set; }
        public int AbsoluteId { get; set; }
        public List<Region> Regions { get; set; }
        public double Height { get; set; }

        public Layer()
        {
            
        }
    }
}
