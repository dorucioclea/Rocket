﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aiwins.Rocket.Emailing.Templates {
    public class EmailTemplateProvider : IEmailTemplateProvider, ITransientDependency {
        protected IEmailTemplateDefinitionManager EmailTemplateDefinitionManager;
        protected ITemplateLocalizer TemplateLocalizer { get; }
        protected RocketEmailTemplateOptions Options { get; }
        protected IStringLocalizerFactory StringLocalizerFactory;

        public EmailTemplateProvider (IEmailTemplateDefinitionManager emailTemplateDefinitionManager,
            ITemplateLocalizer templateLocalizer, IStringLocalizerFactory stringLocalizerFactory,
            IOptions<RocketEmailTemplateOptions> options) {
            EmailTemplateDefinitionManager = emailTemplateDefinitionManager;
            TemplateLocalizer = templateLocalizer;
            StringLocalizerFactory = stringLocalizerFactory;
            Options = options.Value;
        }

        public async Task<EmailTemplate> GetAsync (string name) {
            return await GetAsync (name, CultureInfo.CurrentUICulture.Name);
        }

        public async Task<EmailTemplate> GetAsync (string name, string cultureName) {
            return await GetInternalAsync (name, cultureName);
        }

        protected virtual async Task<EmailTemplate> GetInternalAsync (string name, string cultureName) {
            var emailTemplateDefinition = EmailTemplateDefinitionManager.GetOrNull (name);
            if (emailTemplateDefinition == null) {
                // TODO: Localized message
                throw new RocketException ($"email template {name} not definition");
            }

            var emailTemplateString = emailTemplateDefinition.Contributors.GetOrNull (cultureName);
            if (emailTemplateString == null && emailTemplateDefinition.DefaultCultureName != null) {
                emailTemplateString =
                    emailTemplateDefinition.Contributors.GetOrNull (emailTemplateDefinition.DefaultCultureName);
                if (emailTemplateString != null) {
                    cultureName = emailTemplateDefinition.DefaultCultureName;
                }
            }

            if (emailTemplateString != null) {
                var emailTemplate = new EmailTemplate (emailTemplateString, emailTemplateDefinition);

                await SetLayoutAsync (emailTemplateDefinition, emailTemplate, cultureName);

                if (emailTemplateDefinition.SingleTemplateFile) {
                    await LocalizeAsync (emailTemplateDefinition, emailTemplate, cultureName);
                }

                return emailTemplate;
            }

            // TODO: 考虑本地化消息
            throw new RocketException ($"{cultureName} template not exist!");
        }

        protected virtual async Task SetLayoutAsync (EmailTemplateDefinition emailTemplateDefinition,
            EmailTemplate emailTemplate, string cultureName) {
            var layout = emailTemplateDefinition.Layout;
            if (layout.IsNullOrWhiteSpace ()) {
                return;
            }

            if (layout == EmailTemplateDefinition.DefaultLayoutPlaceHolder) {
                layout = Options.DefaultLayout;
            }

            var layoutTemplate = await GetInternalAsync (layout, cultureName);

            emailTemplate.SetLayout (layoutTemplate);
        }

        protected virtual Task LocalizeAsync (EmailTemplateDefinition emailTemplateDefinition,
            EmailTemplate emailTemplate, string cultureName) {
            if (emailTemplateDefinition.LocalizationResource == null) {
                return Task.CompletedTask;
            }

            var localizer = StringLocalizerFactory.Create (emailTemplateDefinition.LocalizationResource);
            if (cultureName != null) {
                using (CultureHelper.Use (new CultureInfo (cultureName))) {
                    emailTemplate.SetContent (TemplateLocalizer.Localize (localizer, emailTemplate.Content));
                }
            } else {
                emailTemplate.SetContent (
                    TemplateLocalizer.Localize (localizer, emailTemplate.Content)
                );
            }

            return Task.CompletedTask;
        }
    }
}