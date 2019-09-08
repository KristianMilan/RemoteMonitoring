using System;
using Mono.Upnp;
using Mono.Ssdp;
using System.Linq;
using System.Net.NetworkInformation;

namespace Monitoring
{
	public abstract class WebDevice
	{
		Mono.Ssdp.Server _server;// = new Mono.Ssdp.Server ("0.0.0.0");
		public WebDevice ()
		{
		}
		public WebDevice (string name, string address) : this()
		{

			Address = new Uri (address);
			Name = name;
			CreateServer ();
		}

		protected internal virtual void CreateServer()
		{
			var b = new Mono.Ssdp.Client ();
			//var found = b.Browse(string.Format("upnp:{0}", Name));
			var found = b.BrowseAll ();
			found.Client.ServiceAdded += HandleServiceAdded;
			found.Start ();

			_server = new Mono.Ssdp.Server ();
			_server.Announce (string.Format ("upnp:{0}", Name), string.Format ("uuid:RemoteMonitor-device:{0}", Name), Address.ToString ());


		}

		void HandleServiceAdded (object sender, ServiceArgs e)
		{
			var item = e.Usn;
			var srv = e.Service;
			var loc1 = e.Service.GetLocation (0);
			var serviceinf = e.Service.ServiceType;
		}

		public string Name {get;set;}
		public Uri Address { get; set;}

		public virtual void Start()
		{
			if (_server == null)
				CreateServer ();

			if (!_server.Started)
				_server.Start ();
		}

		public virtual void Stop()
		{
			if (_server != null && !_server.Started)
				_server.Stop ();
		}

		public bool IsRunning {
			get {
				if (_server != null)
					return _server.Started;
				else
					return false;
			}
		}
	}
}

