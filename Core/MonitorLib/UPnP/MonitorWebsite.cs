using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public class MonitorWebsite : WebDevice
	{
		static string hostAddress = string.Format("http://{0}:8080",Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First());

		public MonitorWebsite () : base("RemoteMonitorWeb", hostAddress)
		{

		}
	}
}

