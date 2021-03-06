﻿using Aiwins.Rocket.AutoMapper;
using Aiwins.Rocket.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Aiwins.Rocket.TenantManagement {
    [DependsOn (typeof (RocketTenantManagementDomainModule))]
    [DependsOn (typeof (RocketTenantManagementApplicationContractsModule))]
    [DependsOn (typeof (RocketAutoMapperModule))]
    public class RocketTenantManagementApplicationModule : RocketModule {
        public override void ConfigureServices (ServiceConfigurationContext context) {
            context.Services.AddAutoMapperObjectMapper<RocketTenantManagementApplicationModule> ();
            Configure<RocketAutoMapperOptions> (options => {
                options.AddProfile<RocketTenantManagementApplicationAutoMapperProfile> (validate: true);
            });
        }
    }
}