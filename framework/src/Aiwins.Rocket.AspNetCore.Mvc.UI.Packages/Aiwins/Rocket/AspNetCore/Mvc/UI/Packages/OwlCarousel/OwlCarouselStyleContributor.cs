﻿using System.Collections.Generic;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Bundling;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.OwlCarousel
{
    public class OwlCarouselStyleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            //TODO: Theming!
            context.Files.AddIfNotContains("/libs/owl.carousel/assets/owl.carousel.min.css");
            context.Files.AddIfNotContains("/libs/owl.carousel/assets/owl.theme.default.min.css");
            context.Files.AddIfNotContains("/libs/owl.carousel/assets/owl.theme.green.min.css");
        }
    }
}