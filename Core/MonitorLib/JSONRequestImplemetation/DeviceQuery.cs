using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Monitoring
{
	public class DeviceLogJSONResponse : JSONObject
	{
		public DeviceLogJSONResponse() : base()
		{
		}
		public DeviceLogJSONResponse (IEnumerable<DeviceLog> deviceLog, IEnumerable<DeviceMap> maps) : this ()
		{
			Values = new Dictionary<DeviceMap, List<DeviceLog>> ();
			foreach (DeviceMap map in maps.Where(m => deviceLog.Select(d => d.DeviceMapID).Distinct().Contains(m.ID))) {
				Values.Add (map, deviceLog.Where (d => d.DeviceMapID == map.ID).ToList ());
			}
		}

		public Dictionary<DeviceMap, List<DeviceLog>> Values {get;set;}
	}

	public class DeviceQuery :  JSONRequestImplementation<JSONObject>
	{
		private static List<QueryKeyInfo> _keyDetails = new List<QueryKeyInfo>();

		static DeviceQuery()
		{
			_keyDetails.Add (new QueryKeyInfo ("start", "The minimum date you want to filter for.", typeof(DateTime), false));
			_keyDetails.Add (new QueryKeyInfo ("end", "The maximum date you want to filter for.", typeof(DateTime), false));
			_keyDetails.Add (new QueryKeyInfo ("name", "One or more sensor names (you can use '%' as a wildcard in the name)", typeof(string), false));
			_keyDetails.Add (new QueryKeyInfo ("type", "One or more sensor types (if none are specified, all types will be queried)", typeof(string), false));
		}

		#region implemented abstract members of JSONRequestImplementation
		public override JSONObject GetResponse (System.Collections.Generic.Dictionary<string, string[]> requestAttributes)
		{
			bool _parseSuccess = true;
			DateTimeStamp _startDate = DateTimeStamp.MinValue;
			DateTimeStamp _endDate = DateTimeStamp.MaxValue;
			List<string> _tempSensorNames = new List<string> ();
			List<DeviceType> _tempSensorTypes = new List<DeviceType> ();

			StringBuilder sb = new StringBuilder ();

			if (requestAttributes.ContainsKey ("start")) {
				if (!DateTimeStamp.TryParse (requestAttributes["start"].FirstOrDefault(), out _startDate))
					_parseSuccess = false;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append (string.Format ("TimeStamp >= '{0}'", _startDate.ToString())); //'2015-03-09 09:38:00'
			}

			if (requestAttributes.ContainsKey ("end")) {
				if (!DateTimeStamp.TryParse (requestAttributes["end"].FirstOrDefault(), out _endDate))
					_parseSuccess = false;
				if (sb.Length > 0) {
					sb.Append (" AND ");
				}
				sb.Append (string.Format ("TimeStamp <= '{0}'", _endDate.ToString()));
			}

			if (requestAttributes.ContainsKey ("name")) {
				bool firstName = true;
				var nameList = requestAttributes ["name"]; //.FirstOrDefault().Split ("|".ToCharArray ());

				if (nameList.Count () > 0) {
					if (sb.Length > 0) {
						sb.Append (" AND ");
					}
					sb.Append ("DeviceMapID in (select ID from DeviceMap where ");

					foreach (string name in nameList) {
						if (firstName)
							firstName = false;
						else
							sb.Append (" OR ");

						sb.Append (string.Format ("DeviceKey = '{0}'", name));
					}
				
					sb.Append (")");
				}
			}

			if (requestAttributes.ContainsKey ("type")) {
				bool firstType = true;
				var typeList = requestAttributes["type"];

				if (typeList.Count () > 0) {

					if (sb.Length > 0) {
						sb.Append (" AND ");
					}
					sb.Append ("DeviceMapID in (select ID from DeviceMap where DeviceType in (");


					foreach (string devType in typeList){ //typeList) {

						DeviceType t = DeviceType.Temperature;
						try {
							if (!Enum.TryParse<DeviceType> (devType, out t))
								t = (DeviceType)int.Parse (devType);

							if (firstType)
								firstType = false;
							else
								sb.Append (", ");

							sb.Append (((int)t).ToString ());
						} catch {

						}
					}
					sb.Append ("))");
				}
			}


			var conn = ORM.CreateConnection ();
			JSONObject resp = null;
			try
			{
				conn.Open ();
			
				var logs = ORM.Select<DeviceLog> ("DeviceLog", sb.ToString (), ref conn);
				var maps = ORM.Select<DeviceMap> ("DeviceMap", string.Format("ID in (select distinct DeviceMapID from DeviceLog where {0})",sb.ToString()));
	
				if (requestAttributes.ContainsKey("csv") && requestAttributes["csv"][0].Trim().ToUpper() == "TRUE")
					resp = new CSVDeviceResponse(logs, maps);
				else
					resp = new ChartJSGraphDevResponse(logs, maps);
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

	public class CurrentDeviceValueQuery : JSONRequestImplementation<JSONObject>
	{
		private static List<QueryKeyInfo> _keyDetails = new List<QueryKeyInfo>();

		static CurrentDeviceValueQuery()
		{
			_keyDetails.Add (new QueryKeyInfo ("name", "A pipe ('|') separated list of sensor names (you can use '%' as a wildcard in the name)", typeof(string), false));
			_keyDetails.Add (new QueryKeyInfo ("type", "A pipe ('|') separated list of sensor types (if left blank all sensor types will be queries)", typeof(string), false));
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
			DeviceLogJSONResponse resp = null;
			try
			{
				conn.Open ();

				var maps = ORM.Select<DeviceMap> ("DeviceMap", sb.ToString (), ref conn);
				var logs = new List<DeviceLog>();
				foreach(var map in maps)
				{
					MonitoringManager.ReadDevice(map.DeviceKey);
				}
				resp = new DeviceLogJSONResponse(logs, maps);
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

