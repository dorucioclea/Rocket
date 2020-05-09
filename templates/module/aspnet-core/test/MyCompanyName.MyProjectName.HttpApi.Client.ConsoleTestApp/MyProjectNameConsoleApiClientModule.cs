﻿using Aiwins.Rocket.Http.Client.IdentityModel;
using Aiwins.Rocket.Modularity;

namespace MyCompanyName.MyProjectName
{
    [DependsOn(
        typeof(MyProjectNameHttpApiClientModule),
        typeof(RocketHttpClientIdentityModelModule)
        )]
    public class MyProjectNameConsoleApiClientModule : RocketModule
    {
        
    }
}
