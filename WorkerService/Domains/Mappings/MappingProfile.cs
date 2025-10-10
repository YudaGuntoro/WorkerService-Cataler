using Mapster;
using WorkerService.Domains.Models;
using WorkerService.Domains.Dtos;

namespace WorkerService.Domains.Mappings
{
    public class MappingProfile : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 1. Card_No_Master => Card_No_Details_Dto
            config.NewConfig<Card_No_Master, Card_No_Details_Dto>();

            // 2. Machine_Status_Master => Machine_Status_Dto
            config.NewConfig<Machine_Status_Master, Machine_Status_Dto>()
                  .Map(dest => dest.StatusCode, src => src.StatusLabel);

            // 3. CZEC1_Machine_Data_Details => Card_No_Details_Dto
            config.NewConfig<CZEC1_Machine_Data_Details, Card_No_Details_Dto>()
                 /* .Map(dest => dest.ActualProduction, src => src.Actual)*/
                  .Map(dest => dest.MachinePlan, src => src.Plan);

            // 4. Production_Plan_Master => Card_No_Details_Dto
            config.NewConfig<Production_Plan_Master, Card_No_Details_Dto>()
                  .Map(dest => dest.SystemPlan, src => src.PlanQty);

            // 5. CZEC1_Machine_Data_Details => Machine_Status_Dto
            config.NewConfig<CZEC1_Machine_Data_Details, Machine_Status_Dto>()
                  .Map(dest => dest.PCSH, src => src.TargetDay);

           
   


            // 3. CZEC1_Machine_Data_Details => Card_No_Details_Dto
            config.NewConfig<CZEC2_Machine_Data_Details, Card_No_Details_Dto>()
                  /* .Map(dest => dest.ActualProduction, src => src.Actual)*/
                  .Map(dest => dest.MachinePlan, src => src.Plan);


            // 5. CZEC1_Machine_Data_Details => Machine_Status_Dto
            config.NewConfig<CZEC2_Machine_Data_Details, Machine_Status_Dto>()
                  .Map(dest => dest.PCSH, src => src.TargetDay);

        }
    }
}
