using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.MailSettings.Backoffice.Models
{
	public class SmtpSettings : global::Umbraco.Cms.Core.Configuration.Models.SmtpSettings
    {

        public string Environment { get; set; }

		public bool SaveToCurrent { get; set; } = false;
	}
}
