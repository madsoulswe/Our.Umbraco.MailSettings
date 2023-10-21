
using Our.Umbraco.MailSettings.Backoffice.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;
using MailKitSecureSocketOptions = MailKit.Security.SecureSocketOptions;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Configuration.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Extensions;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.Models.Email;
using MimeKit;

namespace Our.Umbraco.MailSettings.Backoffice.Api
{

	[PluginController("OurUmbracoMailSettings")]
	//[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
	public class DashboardController : UmbracoAuthorizedJsonController
	{
		private readonly ILogger<DashboardController> _logger;
		private readonly IWebHostEnvironment _environment;
		private readonly IConfigManipulator _configManipulator;
		private readonly IEnvironmentConfigManipulator _environmentConfigManipulator;
		private readonly IEmailSender _emailSender;
		private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
		private GlobalSettings _globalSettings;
		private MailSettingsOptions _mailSettingsOptions;
		public DashboardController(ILogger<DashboardController> logger,
				IWebHostEnvironment environment,
				IOptionsSnapshot<GlobalSettings> options,
				IOptions<MailSettingsOptions> mailSettingsOptions,
				IConfigManipulator configManipulator,
				IEnvironmentConfigManipulator environmentConfigManipulator,
				IEmailSender emailSender,
				IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
		{
			_logger = logger;
			_environment = environment;
			_configManipulator = configManipulator;
			_environmentConfigManipulator = environmentConfigManipulator;
			_emailSender = emailSender;
			_backOfficeSecurityAccessor = backOfficeSecurityAccessor;
			_globalSettings = options.Value;
			_mailSettingsOptions = mailSettingsOptions.Value;
		}

		public bool GetApi() => true;

		[HttpGet]
		public bool IsJsonProvider()
		{
			return _environmentConfigManipulator.IsJsonProvider();
		}

		[HttpGet]
		public bool CanChangeSettings()
		{
			
			if (!_environmentConfigManipulator.IsJsonProvider())
				return false;
			
			var currentHost = _globalSettings.Smtp?.Host;

			//Current smtp-host is empty so we can change settings
			if (currentHost.IsNullOrWhiteSpace())
				return true;
			
			var environmentConfig = _environmentConfigManipulator.GetSmtpSettings(_environment.EnvironmentName)?.ToObject<global::Umbraco.Cms.Core.Configuration.Models.SmtpSettings>();
			var defaultConfig = _environmentConfigManipulator.GetSmtpSettings(null)?.ToObject<global::Umbraco.Cms.Core.Configuration.Models.SmtpSettings>();

			//If the current smtp-host is set but is not same as the default/env smtp-host, then we can not change the settings
			if (_globalSettings.Smtp?.Host != environmentConfig?.Host && _globalSettings.Smtp?.Host != defaultConfig?.Host)
			{
				return false;
			}
			
			return true;
		}

		[HttpGet]
		public IActionResult GetSettings()
		{
			if(!_environmentConfigManipulator.IsJsonProvider())
				throw new Exception("The current configuration provider is not JSON. Please change the configuration provider to JSON in order to use this package.");

			var canChangeSettings = _mailSettingsOptions.ReadOnly ? false : CanChangeSettings();
			
			var hasEnvironmentConfig = canChangeSettings ? _environmentConfigManipulator.HasEnvironmentConfig(_environment.EnvironmentName) : false;
			

			var settings = new Our.Umbraco.MailSettings.Backoffice.Models.SmtpSettings()
			{
				From = _globalSettings.Smtp?.From,
				Port = _globalSettings.Smtp?.Port ?? 587,
				Host = _globalSettings.Smtp?.Host,
				SecureSocketOptions = _globalSettings.Smtp?.SecureSocketOptions ?? SecureSocketOptions.StartTls,
				Username = _globalSettings.Smtp?.Username,
				Password = _mailSettingsOptions.Protected ? "●●●●●●●●●●●●●" : _globalSettings.Smtp?.Password,
				Environment = _environment.EnvironmentName,
				HasEnvironment = hasEnvironmentConfig,
				CanChangeSettings = canChangeSettings
			};

			return new JsonResult(settings);
		}

			
		[HttpPut]
		public async Task<IActionResult> UpdateSettingsAsync(Models.SmtpSettings model)
		{
			if (!CanChangeSettings() && !_mailSettingsOptions.ReadOnly)
				throw new Exception("The current configuration is not appsettings.json. Please change the configuration in order to use this package.");
            
			model.Password = (model.Password ?? "").Trim("●");
            
			if (_mailSettingsOptions.Protected)
				model.Password = _globalSettings.Smtp?.Password;


            if (!model.TestReceiver.IsNullOrWhiteSpace())
				await TestSettings(model, model.TestReceiver);

            model.Password = (model.Password ?? "").Trim("●");

            var settings = JObject.FromObject(new
			{
				model.From,
				model.Port,
				model.Host,
				SecureSocketOptions = model.SecureSocketOptions.ToString(),
				model.Username,
				model.Password,
			}).ToObject<Dictionary<string, object>>();

			if (_mailSettingsOptions.Protected)
				settings.Remove("Password");


			_environmentConfigManipulator.SaveSmtpSettings(settings, model.SaveToCurrent ? _environment.EnvironmentName : null);


			//Wait for the file to be saved and globalsettings to be updated
			Thread.Sleep(500);

			return new JsonResult(_globalSettings.Smtp);
		}

		[HttpPost]
		public async Task<IActionResult> SendTestAsync(TestMail model)
		{
			model.Sender ??= _globalSettings.Smtp.From;
			model.Subject ??= "Test mail from Umbraco";
			model.Body ??= "Test mail from Umbraco";

			var message = new EmailMessage(model.Sender, model.Receiver, model.Subject, model.Body, false);

			await _emailSender.SendAsync(message, emailType: "TestMail");

			return new JsonResult(1);
		}

		private async Task TestSettings(Models.SmtpSettings settings, string receiver)
		{
			var message = new MimeMessage()
			{
				Subject = "Test mail from Mail Settings",
				Body = new TextPart(MimeKit.Text.TextFormat.Text)
				{
					Text = "Test mail from Mail Settings"
				},
				From = { new MailboxAddress(settings.From, settings.From) },
				To = { new MailboxAddress(receiver, receiver) }
			};
			
			using var client = new MailKitSmtpClient();

			await client.ConnectAsync(
				settings!.Host,
				settings.Port,
				(MailKitSecureSocketOptions)(int)settings.SecureSocketOptions);

			if (!string.IsNullOrWhiteSpace(settings.Username) &&
				!string.IsNullOrWhiteSpace(settings.Password))
			{
				await client.AuthenticateAsync(_globalSettings.Smtp.Username, _globalSettings.Smtp.Password);
			}
			
			try
			{
                var response = await client.SendAsync(message);
            } catch (Exception e)
			{
                throw new Exception(e.Message);
            }
			


			await client.DisconnectAsync(true);
		}
		
	}
	
}
