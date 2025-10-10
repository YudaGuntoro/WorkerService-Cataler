using Mapster;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.CoatWidthControl;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.DTOs.MasterData;   // <-- dto CoatWidthControlDto
using Web.API.Mappings.Export;
using Web.API.Mappings.Request;

namespace Web.API.Mappings.Mappings
{
    public static class MappingConfig
    {
        public static void RegisterMappings()
        {
            // AlarmLogHistory -> GetAlarmLogDto
            TypeAdapterConfig<AlarmLogHistory, GetAlarmLogDto>.NewConfig()
                .Map(dest => dest.LineNo, src => src.LineNo)
                .Map(dest => dest.Timestamp, src => src.Timestamp)
                .Ignore(dest => dest.LineName); // di-enrich dari LineMaster

            // Excel -> CardNoMaster
            TypeAdapterConfig<CardNoMasterUploadExcel, CardNoMaster>.NewConfig()
                .Map(dest => dest.LineNo, src => src.LINE_NO)
                .Map(dest => dest.LineName, src => src.LINE_NAME)
                .Map(dest => dest.CardNo, src => src.CARD_NO)
                .Map(dest => dest.ProductName, src => src.PRODUCT_NM)
                .Map(dest => dest.MaterialName, src => src.MATERIAL_NM)
                .Map(dest => dest.PartNo, src => src.PART_NO)
                .Map(dest => dest.MaterialNo, src => src.MATERIAL_NO)
                .Map(dest => dest.SubstrateName, src => src.SUBSTRATE_NM)
                .Map(dest => dest.TactTime, src => src.TACT_TIME)
                .Map(dest => dest.PassHour, src => src.PASS_HOUR)
                .Map(dest => dest.CoatWidthMin, src => src.COAT_WIDTH_MIN)
                .Map(dest => dest.CoatWidthTarget, src => src.COAT_WIDTH_TARGET)
                .Map(dest => dest.CoatWidthMax, src => src.COAT_WIDTH_MAX)
                .Map(dest => dest.SolidityMin, src => src.SOLIDITY_MIN)
                .Map(dest => dest.SolidityTarget, src => src.SOLIDITY_TARGET)
                .Map(dest => dest.SolidityMax, src => src.SOLIDITY_MAX)
                .Map(dest => dest.Viscosity100Min, src => src.VISCOSITY_100_MIN)
                .Map(dest => dest.Viscosity100Max, src => src.VISCOSITY_100_MAX)
                .Map(dest => dest.Viscosity1Min, src => src.VISCOSITY_1_MIN)
                .Map(dest => dest.Viscosity1Max, src => src.VISCOSITY_1_MAX)
                .Map(dest => dest.PHmin, src => src.pH_MIN)
                .Map(dest => dest.PHmax, src => src.pH_Max);
            // =========================
            // CoatWidthControl mappings
            // =========================

            // Request -> Entity (Create/Update)
            TypeAdapterConfig<CoatWidthControlCreate, CoatWidthControl>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt);

            // Entity -> DTO (response)
            TypeAdapterConfig<CoatWidthControl, CoatWidthControlDto>.NewConfig()
                // LineName & ProductName akan di-enrich terpisah (join ke master)
                .Ignore(dest => dest.LineName)
                .Ignore(dest => dest.ProductName);

            TypeAdapterConfig<CoatWidthControl, CoatWidthControlDto>
            .NewConfig()
            .Map(dest => dest.LineName, src => src.LineMaster.LineName) // dari navigation
            .Map(dest => dest.ProductName, src => src.SubProductName ?? "Unknown");

            TypeAdapterConfig<CoatWidthControl, CoatWidthControlDto>
           .NewConfig()
           // coalesce & konversi tipe agar cocok dengan DTO
           .Map(d => d.CoatingNo, s => s.CoatingNo ?? 0)
           .Map(d => d.RecordDate, s => s.RecordDate ?? default(DateOnly))
           .Map(d => d.KpaRecommend, s => (decimal?)s.KpaRecommend)
           .Map(d => d.CoatingPressureKpa, s => (decimal?)s.CoatingPressureKpa)
           .Map(d => d.CoatWidthAvg, s => (decimal?)s.CoatWidthAvg)
           .Map(d => d.KpaAccuracy, s => (decimal?)s.KpaAccuracy)
           .Map(d => d.ProdMemberId, s => s.ProdMemberId ?? 0)
           .Map(d => d.ProdStaffId, s => s.ProdStaffId ?? 0)

           // enrich
           .Map(d => d.LineName, s => s.LineMaster.LineName)
           .Map(d => d.ProductName, s => s.SubProductName ?? "Unknown");
        }
    }
}
