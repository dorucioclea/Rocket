using Aiwins.Rocket.EntityFrameworkCore;
using Aiwins.Rocket.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Aiwins.Rocket.SettingManagement.EntityFrameworkCore {
    [DependsOn (
        typeof (RocketSettingManagementDomainModule),
        typeof (RocketEntityFrameworkCoreModule)
    )]
    public class RocketSettingManagementEntityFrameworkCoreModule : RocketModule {
        public override void ConfigureServices (ServiceConfigurationContext context) {
            context.Services.AddRocketDbContext<SettingManagementDbContext> (options => {
                options.AddDefaultRepositories<ISettingManagementDbContext> ();

                options.AddRepository<Setting, EfCoreSettingRepository> ();
            });
        }
    }
}