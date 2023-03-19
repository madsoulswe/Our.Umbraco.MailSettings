using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Our.Umbraco.MailSettings.Backoffice.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Infrastructure.Mail;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;
using static Lucene.Net.Store.Lock;
using SmtpSettings = Our.Umbraco.MailSettings.Backoffice.Models.SmtpSettings;

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
		private GlobalSettings _globalSettings;

		public DashboardController(ILogger<DashboardController> logger,
				IWebHostEnvironment environment,
				IOptionsSnapshot<GlobalSettings> options,
				IConfigManipulator configManipulator,
				IEnvironmentConfigManipulator environmentConfigManipulator,
				IEmailSender emailSender
			)
		{
			_logger = logger;
			_environment = environment;
			_configManipulator = configManipulator;
			_environmentConfigManipulator = environmentConfigManipulator;
			_emailSender = emailSender;
			_globalSettings = options.Value;
		}

		public bool GetApi() => true;

		[HttpGet]
		public IActionResult GetSettings()
		{
			var settings = new SmtpSettings()
			{
				From = _globalSettings.Smtp.From,
				Port = _globalSettings.Smtp.Port,
				Host = _globalSettings.Smtp.Host,
				SecureSocketOptions = _globalSettings.Smtp.SecureSocketOptions,
				PickupDirectoryLocation = _globalSettings.Smtp.PickupDirectoryLocation,
				DeliveryMethod = _globalSettings.Smtp.DeliveryMethod,
				Username = _globalSettings.Smtp.Username,
				Password = _globalSettings.Smtp.Password,
				Environment = _environment.EnvironmentName
			};

			return new JsonResult(settings);
		}

			
		[HttpPut]
		public IActionResult UpdateSettings(SmtpSettings model)
		{
			var currentSettings = _globalSettings.Smtp;
			var settings = JObject.FromObject(new
			{
				From = model.From,
				Port = model.Port,
				Host = model.Host,
				SecureSocketOptions = model.SecureSocketOptions,
				PickupDirectoryLocation = model.PickupDirectoryLocation,
				DeliveryMethod = model.DeliveryMethod,
				Username = model.Username,
				Password = model.Password,
			}).ToObject<Dictionary<string, object>>();


			_environmentConfigManipulator.SaveSmtpSettings(settings, model.SaveToCurrent ? _environment.EnvironmentName : null);

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
	}
	
}
