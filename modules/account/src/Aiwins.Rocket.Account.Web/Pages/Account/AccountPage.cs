﻿using Aiwins.Rocket.Account.Localization;
using Aiwins.Rocket.AspNetCore.Mvc.UI.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace Aiwins.Rocket.Account.Web.Pages.Account {
    public abstract class AccountPage : RocketPage {
        [RazorInject]
        public IHtmlLocalizer<AccountResource> L { get; set; }
    }
}