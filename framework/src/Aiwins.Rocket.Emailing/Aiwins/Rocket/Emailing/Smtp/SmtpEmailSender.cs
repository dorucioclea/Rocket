using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Aiwins.Rocket.BackgroundJobs;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Threading;

namespace Aiwins.Rocket.Emailing.Smtp {
    /// <summary>
    /// SMTP协议发送邮件
    /// </summary>
    public class SmtpEmailSender : EmailSenderBase, ISmtpEmailSender, ITransientDependency {
        protected ISmtpEmailSenderConfiguration SmtpConfiguration { get; }

        /// <summary>
        /// 创建一个新的SMTP邮件发送 <see cref="SmtpEmailSender"/> 实例
        /// </summary>
        public SmtpEmailSender (
            ISmtpEmailSenderConfiguration smtpConfiguration,
            IBackgroundJobManager backgroundJobManager) : base (smtpConfiguration, backgroundJobManager) {
            SmtpConfiguration = smtpConfiguration;
        }

        public async Task<SmtpClient> BuildClientAsync () {
            var host = await SmtpConfiguration.GetHostAsync ();
            var port = await SmtpConfiguration.GetPortAsync ();

            var smtpClient = new SmtpClient (host, port);

            try {
                if (await SmtpConfiguration.GetEnableSslAsync ()) {
                    smtpClient.EnableSsl = true;
                }

                if (await SmtpConfiguration.GetUseDefaultCredentialsAsync ()) {
                    smtpClient.UseDefaultCredentials = true;
                } else {
                    smtpClient.UseDefaultCredentials = false;

                    var userName = await SmtpConfiguration.GetUserNameAsync ();
                    if (!userName.IsNullOrEmpty ()) {
                        var password = await SmtpConfiguration.GetPasswordAsync ();
                        var domain = await SmtpConfiguration.GetDomainAsync ();
                        smtpClient.Credentials = !domain.IsNullOrEmpty () ?
                            new NetworkCredential (userName, password, domain) :
                            new NetworkCredential (userName, password);
                    }
                }

                return smtpClient;
            } catch {
                smtpClient.Dispose ();
                throw;
            }
        }

        protected override async Task SendEmailAsync (MailMessage mail) {
            using (var smtpClient = await BuildClientAsync ()) {
                await smtpClient.SendMailAsync (mail);
            }
        }
    }
}