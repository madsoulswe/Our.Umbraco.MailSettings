using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Our.Umbraco.MailSettings
{
	public interface IEnvironmentConfigManipulator
	{
		ILogger<EnvironmentConfigManipulator> Logger { get; }

		JObject GetSmtpSettings(string env = null);
		bool HasEnvironmentConfig(string env);
		bool IsJsonProvider();
		void SaveSmtpSettings(Dictionary<string, object> values, string env = null);
	}
}