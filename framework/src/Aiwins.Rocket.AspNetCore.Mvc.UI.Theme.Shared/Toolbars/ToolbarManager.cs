﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Theming;
using Aiwins.Rocket.DependencyInjection;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Theme.Shared.Toolbars
{
    public class ToolbarManager : IToolbarManager, ITransientDependency
    {
        protected IThemeManager ThemeManager { get; }
        protected RocketToolbarOptions Options { get; }
        protected IServiceProvider ServiceProvider { get; }

        public ToolbarManager(
            IOptions<RocketToolbarOptions> options, 
            IServiceProvider serviceProvider,
            IThemeManager themeManager)
        {
            ThemeManager = themeManager;
            ServiceProvider = serviceProvider;
            Options = options.Value;
        }

        public async Task<Toolbar> GetAsync(string name)
        {
            var toolbar = new Toolbar(name);

            using (var scope = ServiceProvider.CreateScope())
            {
                var context = new ToolbarConfigurationContext(ThemeManager.CurrentTheme, toolbar, scope.ServiceProvider);

                foreach (var contributor in Options.Contributors)
                {
                    await contributor.ConfigureToolbarAsync(context);
                }
            }

            return toolbar;
        }
    }
}