﻿using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Aiwins.Rocket.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form
{
    [HtmlTargetElement("rocket-dynamic-form", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class RocketDynamicFormTagHelper : RocketTagHelper<RocketDynamicFormTagHelper, RocketDynamicFormTagHelperService>
    {
        [HtmlAttributeName("rocket-model")]
        public ModelExpression Model { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
        
        public bool? SubmitButton { get; set; }

        public bool? RequiredSymbols { get; set; } = true;

#region MvcFormTagHelperAttiributes

        private const string ActionAttributeName = "asp-action";
        private const string AreaAttributeName = "asp-area";
        private const string PageAttributeName = "asp-page";
        private const string PageHandlerAttributeName = "asp-page-handler";
        private const string FragmentAttributeName = "asp-fragment";
        private const string ControllerAttributeName = "asp-controller";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";

        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        [HtmlAttributeName(AreaAttributeName)]
        public string Area { get; set; }

        [HtmlAttributeName(PageAttributeName)]
        public string Page { get; set; }

        [HtmlAttributeName(PageHandlerAttributeName)]
        public string PageHandler { get; set; }

        [HtmlAttributeName(FragmentAttributeName)]
        public string Fragment { get; set; }

        [HtmlAttributeName(RouteAttributeName)]
        public string Route { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Method { get; set; }

        [HtmlAttributeName(RouteValuesDictionaryName, DictionaryAttributePrefix = RouteValuesPrefix)]
        public IDictionary<string, string> RouteValues { get; set; }

#endregion

        public RocketDynamicFormTagHelper(RocketDynamicFormTagHelperService tagHelperService)
            : base(tagHelperService)
        {

        }
    }
}