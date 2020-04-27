﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Aiwins.Rocket.Http.Modeling;
using Aiwins.Rocket.Http.ProxyScripting.Generators;
using Aiwins.Rocket.Localization;
using Aiwins.Rocket.Reflection;
using JetBrains.Annotations;

namespace Aiwins.Rocket.Http.Client.DynamicProxying {
    internal static class UrlBuilder {
        public static string GenerateUrlWithParameters (ActionApiDescriptionModel action, IReadOnlyDictionary<string, object> methodArguments, ApiVersionInfo apiVersion) {
            var urlBuilder = new StringBuilder (action.Url);

            ReplacePathVariables (urlBuilder, action.Parameters, methodArguments, apiVersion);
            AddQueryStringParameters (urlBuilder, action.Parameters, methodArguments, apiVersion);

            return urlBuilder.ToString ();
        }

        private static void ReplacePathVariables (StringBuilder urlBuilder, IList<ParameterApiDescriptionModel> actionParameters, IReadOnlyDictionary<string, object> methodArguments, ApiVersionInfo apiVersion) {
            var pathParameters = actionParameters
                .Where (p => p.BindingSourceId == ParameterBindingSources.Path)
                .ToArray ();

            if (!pathParameters.Any ()) {
                return;
            }

            if (pathParameters.Any (p => p.Name == "apiVersion")) {
                urlBuilder = urlBuilder.Replace ("{apiVersion}", apiVersion.Version);
            }

            foreach (var pathParameter in pathParameters.Where (p => p.Name != "apiVersion")) //TODO: Constant!
            {
                var value = HttpActionParameterHelper.FindParameterValue (methodArguments, pathParameter);

                if (value == null) {
                    if (pathParameter.IsOptional) {
                        urlBuilder = urlBuilder.Replace ($"{{{pathParameter.Name}}}", "");
                    } else if (pathParameter.DefaultValue != null) {
                        urlBuilder = urlBuilder.Replace ($"{{{pathParameter.Name}}}", pathParameter.DefaultValue.ToString ());
                    } else {
                        throw new RocketException ($"Missing path parameter value for {pathParameter.Name} ({pathParameter.NameOnMethod})");
                    }
                } else {
                    urlBuilder = urlBuilder.Replace ($"{{{pathParameter.Name}}}", value.ToString ());
                }
            }
        }

        private static void AddQueryStringParameters (StringBuilder urlBuilder, IList<ParameterApiDescriptionModel> actionParameters, IReadOnlyDictionary<string, object> methodArguments, ApiVersionInfo apiVersion) {
            var queryStringParameters = actionParameters
                .Where (p => p.BindingSourceId.IsIn (ParameterBindingSources.ModelBinding, ParameterBindingSources.Query))
                .ToArray ();

            var isFirstParam = true;

            foreach (var queryStringParameter in queryStringParameters) {
                var value = HttpActionParameterHelper.FindParameterValue (methodArguments, queryStringParameter);
                if (value == null) {
                    continue;
                }

                AddQueryStringParameter (urlBuilder, isFirstParam, queryStringParameter.Name, value);

                isFirstParam = false;
            }

            if (apiVersion.ShouldSendInQueryString ()) {
                AddQueryStringParameter (urlBuilder, isFirstParam, "api-version", apiVersion.Version); //TODO: Constant!
            }
        }

        private static void AddQueryStringParameter (
            StringBuilder urlBuilder,
            bool isFirstParam,
            string name, [NotNull] object value) {
            urlBuilder.Append (isFirstParam ? "?" : "&");

            urlBuilder.Append (name + "=" + System.Net.WebUtility.UrlEncode (ConvertValueToString (value)));
        }

        private static string ConvertValueToString ([NotNull] object value) {
            using (CultureHelper.Use (CultureInfo.InvariantCulture)) {
                
                if (value is DateTime dateTimeValue) {
                    return dateTimeValue.ToUniversalTime ().ToString ("u");
                }

                if (value is DateTimeOffset dateTimeOffsetValue) {
                    return dateTimeOffsetValue.ToUniversalTime ().ToString ("u");
                }

                return value.ToString ();
            }
        }
    }
}