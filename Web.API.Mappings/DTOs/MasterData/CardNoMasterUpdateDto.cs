using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.MasterData
{
    public class CardNoMasterUpdateDto
    {
        public string LineNo { get; set; } = null!;

        public string LineName { get; set; } = null!;

        public string CardNo { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public string MaterialName { get; set; } = null!;

        public string PartNo { get; set; } = null!;

        public string MaterialNo { get; set; } = null!;

        public string SubstrateName { get; set; } = null!;

        public string TactTime { get; set; } = null!;

        public string PassHour { get; set; } = null!;

        public string CoatWidthMin { get; set; } = null!;

        public string CoatWidthTarget { get; set; } = null!;

        public string CoatWidthMax { get; set; } = null!;

        public string SolidityMin { get; set; } = null!;

        public string SolidityTarget { get; set; } = null!;

        public string SolidityMax { get; set; } = null!;

        public string Viscosity100Min { get; set; } = null!;

        public string Viscosity100Max { get; set; } = null!;

        public string Viscosity1Min { get; set; } = null!;

        public string Viscosity1Max { get; set; } = null!;

        public string PHmin { get; set; } = null!;

        public string PHmax { get; set; } = null!;

        public string? _4wMembers { get; set; }

        public string? _4wStaffSo { get; set; }
    }
}
