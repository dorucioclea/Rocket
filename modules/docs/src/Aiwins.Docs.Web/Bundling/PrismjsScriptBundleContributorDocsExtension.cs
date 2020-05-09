﻿using System.Collections.Generic;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Bundling;

namespace Aiwins.Docs.Bundling
{
    public class PrismjsScriptBundleContributorDocsExtension : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/libs/prismjs/plugins/toolbar/prism-toolbar.js");
            context.Files.AddIfNotContains("/libs/prismjs/plugins/show-language/prism-show-language.js");
            context.Files.AddIfNotContains("/libs/prismjs/plugins/copy-to-clipboard/prism-copy-to-clipboard.js");
            context.Files.AddIfNotContains("/libs/prismjs/plugins/line-highlight/prism-line-highlight.js");
            context.Files.AddIfNotContains("/libs/prismjs/components/prism-csharp.js");
        }
    }
}