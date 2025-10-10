using Web.API.Persistence.Helper;
using Web.API.Persistence.Repository;
using Web.API.Persistence.Services;

namespace Web.API
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ILogCycleService, LogCycleService>();
            services.AddScoped<IProductionPlanService, ProductionPlanService>();
            services.AddScoped<ILogAlarmService, LogAlarmService>();
            services.AddScoped<IMasterDataService, MasterDataService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICoatWidthControlService, CoatWidthControlService>();
            services.AddScoped<IFacilityCountService, FacilityCountService>();
            services.AddScoped<IProductionCountService, ProductionCountService>();

            //=======================================================================
            services.AddSingleton<IDeviceMapProvider, Machine1DeviceMapProvider>();
            services.AddSingleton<IDeviceMapProvider, Machine2DeviceMapProvider>();

        }
    }
}
