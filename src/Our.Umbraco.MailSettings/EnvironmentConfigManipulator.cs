using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;
using NUglify.Helpers;
using Umbraco.Cms.Core.Configuration.Models;

namespace Our.Umbraco.MailSettings
{
	public class EnvironmentConfigManipulator : IEnvironmentConfigManipulator
	{
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;
		private readonly ILogger<EnvironmentConfigManipulator> _logger;
		private readonly object _locker = new object();

		public EnvironmentConfigManipulator(
			IWebHostEnvironment environment, 
			IConfiguration configuration, 
			ILogger<EnvironmentConfigManipulator> logger)
		{

			_environment = environment;
			_configuration = configuration;
			_logger = logger;
		}

		public ILogger<EnvironmentConfigManipulator> Logger { get; }

		public bool IsJsonProvider() => GetJsonConfigurationProvider() != null;

		

		public JObject GetSmtpSettings(string env = null)
		{
			var provider = GetJsonConfigurationProvider();

			return GetJson(provider, env)?["Umbraco"]?["CMS"]?["Global"]?["Smtp"] as JObject;
		}

		
		
		public void SaveSmtpSettings(Dictionary<string, object>values, string env = null)
		{
			// Save key to JSON
			var provider = GetJsonConfigurationProvider();

			var json = GetJson(provider, env);
			if (json is null)
			{
				_logger.LogWarning("Failed to save configuration in JSON configuration.");
				return;
			}

			foreach (var key in values.Keys)
			{
				var value = values[key];
				var path = key.Split(new[] { ':' });
				var propertyName = path.Last();

				var item = GetSmtpItem(propertyName, value);
				if (item is not null)
				{
					json.Merge(item, new JsonMergeSettings());
				}
			}
			

			SaveJson(provider, json, env);
		}

		private object? GetSmtpItem(string property, object value)
		{
			JTokenWriter writer = new JTokenWriter();

			writer.WriteStartObject();
			writer.WritePropertyName("Umbraco");
				writer.WriteStartObject();
				writer.WritePropertyName("CMS");
					writer.WriteStartObject();
					writer.WritePropertyName("Global");
						writer.WriteStartObject();
							writer.WritePropertyName("Smtp");
							writer.WriteStartObject();
								writer.WritePropertyName(property);
								writer.WriteValue(value);
						writer.WriteEndObject();
					writer.WriteEndObject();
				writer.WriteEndObject();
			writer.WriteEndObject();

			return writer.Token;
		}


		public bool HasEnvironmentConfig(string env)
		{
			var provider = GetJsonConfigurationProvider();

			if (provider.Source.FileProvider is PhysicalFileProvider physicalFileProvider)
			{
				var path = Path.Combine(physicalFileProvider.Root, provider.Source.Path).Replace(".json", $".{env}.json");
				return File.Exists(path);
			}

			return false;
		}


		private void SaveJson(JsonConfigurationProvider? provider, JObject? json, string env)
		{
			if (provider is null)
			{
				throw new Exception("No JSON configuration provider found.");
			}

			lock (_locker)
			{
				if (provider.Source.FileProvider is PhysicalFileProvider physicalFileProvider)
				{
					var jsonFilePath = Path.Combine(physicalFileProvider.Root, provider.Source.Path);

					if (!env.IsNullOrWhiteSpace())
					{
						var envJsonFilePath = jsonFilePath.Replace(".json", $".{env}.json");

						if (File.Exists(envJsonFilePath))
							jsonFilePath = envJsonFilePath;
					}

					try
					{
						using (var sw = new StreamWriter(jsonFilePath, false))
						using (var jsonTextWriter = new JsonTextWriter(sw)
						{
							Formatting = Formatting.Indented,
						})
						{
							json?.WriteTo(jsonTextWriter);
						}
					}
					catch (IOException exception)
					{
						_logger.LogWarning(exception, "JSON configuration could not be written: {path}", jsonFilePath);
					}
				}
			}
		}

		private JObject? GetJson(JsonConfigurationProvider? provider, string env)
		{
			if (provider is null)
			{
				throw new Exception("No JSON configuration provider found.");
			}

			lock (_locker)
			{
				if (provider.Source.FileProvider is not PhysicalFileProvider physicalFileProvider)
				{
					return null;
				}

				var jsonFilePath = Path.Combine(physicalFileProvider.Root, provider.Source.Path);

				if (!env.IsNullOrWhiteSpace())
				{
					var envJsonFilePath = jsonFilePath.Replace(".json", $".{env}.json");

					if (File.Exists(envJsonFilePath))
						jsonFilePath = envJsonFilePath;
				}

				try
				{
					var serializer = new JsonSerializer();
					using var sr = new StreamReader(jsonFilePath);
					using var jsonTextReader = new JsonTextReader(sr);
					return serializer.Deserialize<JObject>(jsonTextReader);
				}
				catch (IOException exception)
				{
					_logger.LogWarning(exception, "JSON configuration could not be read: {path}", jsonFilePath);
					return null;
				}
			}
		}


		private JsonConfigurationProvider? GetJsonConfigurationProvider()
		{
			if (_configuration is IConfigurationRoot configurationRoot)
			{
				foreach (var provider in configurationRoot.Providers)
				{
					if (provider is JsonConfigurationProvider jsonConfigurationProvider)
					{
						return jsonConfigurationProvider;
					}
				}
			}

			return null;
		}

		private static JToken? CaseSelectPropertyValues(JToken? token, string name)
		{
			if (token is JObject obj)
			{
				foreach (var property in obj.Properties())
				{
					if (name is null)
					{
						return property.Value;
					}

					if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
					{
						return property.Value;
					}
				}
			}

			return null;
		}
	}


}
