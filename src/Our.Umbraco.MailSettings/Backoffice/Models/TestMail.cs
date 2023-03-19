using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.MailSettings.Backoffice.Models
{
	public class TestMail
	{
		public string? Sender { get; set; }
		public string Receiver { get; set; }
		public string? Subject { get; set; }

		public string? Body { get; set; }
	}
}
