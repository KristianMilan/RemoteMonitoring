using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Monitoring
{
	public class TemperatureJSONResponse : JSONObject
	{
		public TemperatureJSONResponse() : base()
		{
		}
		public TemperatureJSONResponse (IEnumerable<DeviceLog> deviceLog, IEnumerable<DeviceMap> maps) : this ()
		{
			Values = new Dictionary<DeviceMap, List<DeviceLog>> ();
			foreach (DeviceMap map in maps.Where(m => deviceLog.Select(d => d.DeviceMapID).Distinct().Contains(m.ID))) {
				Values.Add (map, deviceLog.Where (d => d.DeviceMapID == map.ID).ToList ());
			}
		}

		public Dictionary<DeviceMap, List<DeviceLog>> Values {get;set;}
	}

	public class TemperatureQuery :  JSONRequestImplementation<JSONObject>
	{
		private static List<QueryKeyInfo> _keyDetails = new List<QueryKeyInfo>();

		static TemperatureQuery()
		{
			_keyDetails.Add (new QueryKeyInfo ("start", "The minimum date you want to filter for.", typeof(DateTime), false));
			_keyDetails.Add (new QueryKeyInfo ("end", "The maximum date you want to filter for.", typeof(DateTime), false));
			_keyDetails.Add (new QueryKeyInfo ("name", "A pipe ('|') separated list of sensor names (you can use '%' as a wildcard in the name)", typeof(string), false));
		}

		#region implemented abstract members of JSONRequestImplementation
		public override JSONObject GetResponse (System.Collections.Generic.Dictionary<string, string[]> requestAttributes)
		{
			bool _parseSuccess = true;
			DateTime _startDate = DateTime.MinValue;
			DateTime _endDate = DateTime.MaxValue;
			List<string> _tempSensorNames = new List<string> ();

			StringBuilder sb = new StringBuilder ();

			if (requestAttributes.ContainsKey ("start")) {
				if (!DateTime.TryParse (requestAttributes ["start"].ToString(), out _startDate))
					_parseSuccess = false;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append (string.Format ("TimeStamp >= '{0}'", _startDate.ToString ()));
			}

			if (requestAttributes.ContainsKey ("end")) {
				if (!DateTime.TryParse (requestAttributes ["end"].ToString(), out _endDate))
					_parseSuccess = false;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append (string.Format ("TimeStamp <= '{0}'", _endDate.ToString ()));
			}

			sb.Append (string.Format("DeviceMapID in (select ID from DeviceMap where DeviceType = {0})",(int)DeviceType.Temperature));

			if (requestAttributes.ContainsKey ("names")) {

				bool firstName = true;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append ("DeviceMapID in (select ID from DeviceMap where ");

				foreach (string name in requestAttributes["names"].ToString().Split("|".ToCharArray())) {
					if (firstName)
						firstName = false;
					else
						sb.Append (" OR ");

					sb.Append (string.Format ("Name like '{0}'", name));
				}
				
				sb.Append (")");
			}

			var conn = ORM.CreateConnection ();
			TemperatureJSONResponse resp = null;
			try
			{
				conn.Open ();
			
				var logs = ORM.Select<DeviceLog> ("DeviceLog", sb.ToString (), ref conn);
				var maps = ORM.Select<DeviceMap> ("DeviceMap", string.Format("ID in (select distinct DeviceMapID from DeviceLog where {0})",sb.ToString()));
	
				resp = new TemperatureJSONResponse(logs, maps);
			}
			catch(Exception ex) {
				LogWriter.WriteLog (string.Format ("An error was encountered wile attempting to get temperature logs for the filter '{0}'", sb.ToString ()), ex);
			}
			conn.Close ();

			if (!_parseSuccess)
				return null;
			else
				return resp;
		}
		public override List<QueryKeyInfo> QueryKeyDetails {
			get {
				return _keyDetails;
			}
		}
		#endregion
	}

	public class CurrentTemperatureQuery : JSONRequestImplementation<JSONObject>
	{
		private static List<QueryKeyInfo> _keyDetails = new List<QueryKeyInfo>();

		static CurrentTemperatureQuery()
		{
			_keyDetails.Add (new QueryKeyInfo ("name", "A pipe ('|') separated list of sensor names (you can use '%' as a wildcard in the name)", typeof(string), false));
		}
		#region implemented abstract members of JSONRequestImplementation
		public override JSONObject GetResponse (Dictionary<string, string[]> requestAttributes)
		{
			bool _parseSuccess = true;
			List<string> _tempSensorNames = new List<string> ();

			StringBuilder sb = new StringBuilder ();

			sb.Append (string.Format("DeviceType = {0}",(int)DeviceType.Temperature));

			if (requestAttributes.ContainsKey ("names")) {

				bool firstName = true;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append ("(");

				foreach (string name in requestAttributes["names"].ToString().Split("|".ToCharArray())) {
					if (firstName)
						firstName = false;
					else
						sb.Append (" OR ");

					sb.Append (string.Format ("Name like '{0}'", name));
				}

				sb.Append (")");
			}

			var conn = ORM.CreateConnection ();
			TemperatureJSONResponse resp = null;
			try
			{
				conn.Open ();

				var maps = ORM.Select<DeviceMap> ("DeviceMap", sb.ToString (), ref conn);
				var logs = new List<DeviceLog>();
				foreach(var map in maps)
				{
					MonitoringManager.ReadDevice(map.DeviceKey);
				}
				resp = new TemperatureJSONResponse(logs, maps);
			}
			catch(Exception ex) {
				LogWriter.WriteLog (string.Format ("An error was encountered while attempting to get temperature logs for the filter '{0}'", sb.ToString ()), ex);
			}
			conn.Close ();

			if (!_parseSuccess)
				return null;
			else
				return resp;
		}
		public override List<QueryKeyInfo> QueryKeyDetails {
			get {
				return _keyDetails;
			}
		}
		#endregion
	}

}

