using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Parsers.Shared.Models
{
    public class ProjectInfo
    {
        [NotMapped]
        [JsonIgnore]
        public Project ProjectLink { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string PrintTime { get; set; }
        public float ProjectHeight { get; set; }
        public float LayerSliceHeight { get; set; }
        public string MaterialName { get; set; }
        public string ManifestPath { get; set; }
        [NotMapped]
        [JsonIgnore]
        public int LayersCount => (int)((float)ProjectHeight / LayerSliceHeight);
        public ProjectType ProjectType { get; set; }
    }
}
