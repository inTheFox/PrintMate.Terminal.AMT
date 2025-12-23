using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LaserLib.Models
{
    public class LaserStatus
    {
        [JsonIgnore]
        public bool IsSuccess { get; set; }

        public string? ver { get; set; }

        [JsonConverter(typeof(OnOffBoolConverter))]
        public bool ROP { get; set; }

        [JsonConverter(typeof(OnOffBoolConverter))]
        public bool RPP { get; set; }

        public double RIMON { get; set; }
        public string? RMT { get; set; }
        public double RCT { get; set; }
        public double RBT { get; set; }
        public int RET { get; set; }
        public int STA { get; set; }

        [JsonIgnore]
        public bool[] STAStates { get; set; } = new bool[32];
        public string? RID { get; set; }
        public string? RFV { get; set; }
        public string? RSN { get; set; }
        public double RCS { get; set; }
        public double RPW { get; set; }
        public double RDC { get; set; }
        public double RDCmax { get; set; }
        public double RPRR { get; set; }
        public int REC { get; set; }
        public int RMEC { get; set; }
        public string? RLHN { get; set; }

        [JsonConverter(typeof(OnOffBoolConverter))]
        public bool RDHCP { get; set; }

        public int FST { get; set; }
        public double RPRRL { get; set; }
        public double RPRRH { get; set; }
        public int RCFG { get; set; }
        public int WFCFG { get; set; }
        public int WFID { get; set; }
        public int LANG_I { get; set; }
        public double RPWMIN { get; set; }
        public double RPWMAX { get; set; }
        public double RNC { get; set; }

        [JsonIgnore]
        private DateTime _update = DateTime.Now;
        [JsonIgnore]
        public string Update => _update.ToString("HH:mm:ss");
    }
}
