﻿using Microsoft.AspNetCore.Razor.TagHelpers;
using Aiwins.Rocket.AspNetCore.Mvc.UI.Bootstrap.Microsoft.AspNetCore.Razor.TagHelpers;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Blockquote
{
    public class RocketBlockquoteParagraphTagHelperService : RocketTagHelperService<RocketBlockquoteParagraphTagHelper>
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.AddClass("mb-0");
        }
        
    }
}