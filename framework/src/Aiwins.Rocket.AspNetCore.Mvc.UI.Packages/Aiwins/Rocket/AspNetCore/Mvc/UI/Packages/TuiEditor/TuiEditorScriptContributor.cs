﻿using System.Collections.Generic;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Bundling;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.Codemirror;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.HighlightJs;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.MarkdownIt;
using Aiwins.Rocket.Modularity;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Packages.TuiEditor
{
    [DependsOn(
        typeof(HighlightJsScriptContributor),
        typeof(CodemirrorScriptContributor),
        typeof(MarkdownItScriptContributor)
    )]
    public class TuiEditorScriptContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/libs/to-mark/to-mark.min.js");

            if (context.FileProvider.GetFileInfo("/libs/tui-code-snippet/tui-code-snippet.min.js").Exists)
            {
                context.Files.AddIfNotContains("/libs/tui-code-snippet/tui-code-snippet.min.js");
            }
            else
            {
                context.Files.AddIfNotContains("/libs/tui-code-snippet/tui-code-snippet.js");
            }

            context.Files.AddIfNotContains("/libs/squire-rte/squire.js");
            context.Files.AddIfNotContains("/libs/tui-editor/tui-editor-Editor.min.js");
        }
    }
}
