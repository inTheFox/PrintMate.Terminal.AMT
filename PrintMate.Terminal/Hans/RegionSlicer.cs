using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectParserTest.Parsers.Shared.Models;
using Region = ProjectParserTest.Parsers.Shared.Models;

namespace HansScannerHost.Models
{
    public class RegionSlicer
    {
        public static Layer GetLayerWithLaserRegionsById(Layer layer, int laserNum)
        {
            return new Layer()
            {
                Id = layer.Id,
                Height = layer.Height,
                Regions = layer.Regions.Where(p=>p.LaserNum == laserNum).ToList()
            };
        }
    }
}
