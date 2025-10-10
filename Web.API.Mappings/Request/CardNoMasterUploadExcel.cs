using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniExcelLibs.Attributes;

namespace Web.API.Mappings.Request
{
    public class CardNoMasterUploadExcel
    {
        public string LINE_NO { get; set; } = null!;
        public string LINE_NAME { get; set; } = null!;
        public string CARD_NO { get; set; } = null!;
        public string PRODUCT_NM { get; set; } = null!;
        public string MATERIAL_NM { get; set; } = null!;
        public string PART_NO { get; set; } = null!;
        public string MATERIAL_NO { get; set; } = null!;
        public string SUBSTRATE_NM { get; set; } = null!;
        public string TACT_TIME { get; set; } = null!;
        public string PASS_HOUR { get; set; } = null!;
        public string COAT_WIDTH_MIN { get; set; } = null!;
        public string COAT_WIDTH_TARGET { get; set; } = null!;
        public string COAT_WIDTH_MAX { get; set; } = null!;
        public string SOLIDITY_MIN { get; set; } = null!;
        public string SOLIDITY_TARGET { get; set; } = null!;
        public string SOLIDITY_MAX { get; set; } = null!;
        public string VISCOSITY_100_MIN { get; set; } = null!;
        public string VISCOSITY_100_MAX { get; set; } = null!;
        public string VISCOSITY_1_MIN { get; set; } = null!;
        public string VISCOSITY_1_MAX { get; set; } = null!;
        public string pH_MIN { get; set; } = null!;
        public string pH_Max { get; set; } = null!;

        public string? _4WMembers { get; set; }

        public string? _4WStaffSo { get; set; }
    }
}
