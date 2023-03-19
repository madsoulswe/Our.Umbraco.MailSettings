using Microsoft.AspNetCore.Routing;
using Our.Umbraco.MailSettings.Backoffice.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.WebAssets;
using Umbraco.Extensions;


namespace Our.Umbraco.MailSettings
{
	internal class ServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
	{
		private readonly LinkGenerator _linkGenerator;

		public ServerVariablesHandler(LinkGenerator linkGenerator)
		{
			_linkGenerator = linkGenerator;
		}

		public void Handle(ServerVariablesParsingNotification notification)
		{
			notification.ServerVariables.Add("OurUmbracoMailSettings", new Dictionary<string, object>
			{
				{ "api", _linkGenerator.GetUmbracoApiServiceBaseUrl<DashboardController>(controller => controller.GetApi()) }
			});
		}
	}
}
