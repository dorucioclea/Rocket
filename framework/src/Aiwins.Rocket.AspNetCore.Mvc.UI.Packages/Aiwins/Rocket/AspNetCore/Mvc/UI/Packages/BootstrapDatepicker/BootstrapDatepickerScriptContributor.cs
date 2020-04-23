﻿using System.Collections.Generic;
using System.Globalization;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Bundling;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.JQuery;
using Aiwins.Rocket.Modularity;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.Timeago
{
    [DependsOn(typeof(JQueryScriptContributor))]
    public class BootstrapDatepickerScriptContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/libs/bootstrap-datepicker/bootstrap-datepicker.min.js");
        }

        public override void ConfigureDynamicResources(BundleConfigurationContext context)
        {
            var cultureName = CultureInfo.CurrentUICulture.DateTimeFormat.Calendar.AlgorithmType ==
                              CalendarAlgorithmType.LunarCalendar
                ? "en"
                : CultureInfo.CurrentUICulture.Name;

            if (TryAddCultureFile(context, MapCultureName(cultureName)))
            {
                return;
            }
        }

        protected virtual bool TryAddCultureFile(BundleConfigurationContext context, string cultureName)
        {
            var filePath = $"/libs/bootstrap-datepicker/locales/bootstrap-datepicker.{cultureName}.min.js";

            if (!context.FileProvider.GetFileInfo(filePath).Exists)
            {
                return false;
            }

            context.Files.AddIfNotContains(filePath);
            return true;
        }
        
        protected virtual string MapCultureName(string cultureName)
        {
            return cultureName;
        }
    }
}