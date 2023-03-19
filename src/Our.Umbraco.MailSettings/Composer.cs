using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.WebAssets;

namespace Our.Umbraco.MailSettings
{
	public class MailSettingsComposer : IComposer
	{
		public void Compose(IUmbracoBuilder builder)
		{
			builder.Services.AddSingleton<IEnvironmentConfigManipulator, EnvironmentConfigManipulator>();
			builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesHandler>();
		}
	}
}
