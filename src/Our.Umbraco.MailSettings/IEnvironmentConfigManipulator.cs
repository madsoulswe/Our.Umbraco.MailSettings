using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Our.Umbraco.MailSettings
{
	public interface IEnvironmentConfigManipulator
	{
		ILogger<EnvironmentConfigManipulator> Logger { get; }

		void SaveSmtpSettings(Dictionary<string, object> values, string env = null);
	}
}