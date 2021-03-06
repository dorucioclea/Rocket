﻿using Aiwins.Rocket.AutoMapper;
using Aiwins.Rocket.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Aiwins.Rocket.BackgroundJobs {
    [DependsOn (
        typeof (RocketBackgroundJobsDomainSharedModule),
        typeof (RocketBackgroundJobsModule),
        typeof (RocketAutoMapperModule)
    )]
    public class RocketBackgroundJobsDomainModule : RocketModule {
        public override void ConfigureServices (ServiceConfigurationContext context) {
            context.Services.AddAutoMapperObjectMapper<RocketBackgroundJobsDomainModule> ();
            Configure<RocketAutoMapperOptions> (options => {
                options.AddProfile<BackgroundJobsDomainAutoMapperProfile> (validate: true);
            });
        }
    }
}