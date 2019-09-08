using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public class DashboardJSONValues : JSONObject
	{
		public DashboardJSONValues() : base()
		{
		}

		public DashboardJSONValues(IEnumerable<DashboardJSONValue> values) : this()
		{
			Values = values;
		}

		public IEnumerable<DashboardJSONValue> Values {get;private set;}
	}
	public class DashboardJSONValue : JSONObject
	{
		public DashboardJSONValue() : base()
		{
		}

		public DashboardJSONValue(string key, object value, object min, object max, string name, string suffix) : this()
		{
			DeviceKey = key;
			Value = value;
			Min = min;
			Max = max;
			Name = name;
			Suffix = suffix;
		}
		public string DeviceKey{ get; private set;}
		public object Value {get;private set;}
		public object Min {get;private set;}
		public object Max {get;private set;}
		public string Name {get;private set;}
		public string Suffix {get;private set;}
	}

	public class DashboardQuery : JSONRequestImplementation<DashboardJSONValues>
	{
		public DashboardQuery ()
		{
		}

		#region implemented abstract members of JSONRequestImplementation

		public override DashboardJSONValues GetResponse (System.Collections.Generic.Dictionary<string, string[]> requestAttributes)
		{

			var keys = MonitoringManager.DashboardDeviceKeys;

			if (requestAttributes.ContainsKey ("keys")) {
				var requestedKeys = requestAttributes ["keys"].ToString ().Split ("|".ToCharArray ());
				keys = keys.Where (k => requestedKeys.Contains (k));
			}

			return new DashboardJSONValues (keys.Select(k => new DashboardJSONValue(k, MonitoringManager.ReadDevice(k), MonitoringManager.Min(k), MonitoringManager.Max(k), MonitoringManager.Name(k), MonitoringManager.Suffix(k))));
		}

		public override List<QueryKeyInfo> QueryKeyDetails {
			get {
				List<QueryKeyInfo> respInfo = new List<QueryKeyInfo> ();

				respInfo.Add ( new QueryKeyInfo ("keys", "A pipe separated list of keys to include.  If empty, all available devices will be returned.", typeof(string), false));

				return respInfo;
			}
		}

		#endregion
	}
}

