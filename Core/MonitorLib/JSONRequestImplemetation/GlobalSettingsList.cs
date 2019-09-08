using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Monitoring
{
	public class SettingsReturn : JSONObject
	{
		public SettingsReturn() : base()
		{
		}
		public SettingsReturn(IEnumerable<Configuration> configurations) : this()
		{
			Configurations = configurations;
		}

		public IEnumerable<Configuration> Configurations{get;set;}
	}

	public class GlobalSettingsList : JSONRequestImplementation<JSONObject>
	{
		public GlobalSettingsList ()
		{
		}

		#region implemented abstract members of JSONRequestImplementation

		public override JSONObject GetResponse (System.Collections.Generic.Dictionary<string, string[]> requestAttributes)
		{
			bool _parseSuccess = false;
			StringBuilder sb = new StringBuilder ();

			if (requestAttributes.ContainsKey ("key")) {
				bool firstKey = true;
				var nameList = requestAttributes ["key"]; //.FirstOrDefault().Split ("|".ToCharArray ());

				if (nameList.Count () > 0) {
					if (sb.Length > 0) {
						sb.Append (" AND ");
					}
					sb.Append ("(");

					foreach (string name in nameList) {
						if (firstKey)
							firstKey = false;
						else
							sb.Append (" OR ");

						sb.Append (string.Format ("Key like '{0}'", name));
					}

					sb.Append (")");
				}
			}

			if (requestAttributes.ContainsKey ("id")) {
				var nameList = requestAttributes ["id"]; //.FirstOrDefault().Split ("|".ToCharArray ());

				if (nameList.Count () > 0) {
					if (sb.Length > 0) {
						sb.Append (" AND ");
					}
					sb.Append ("(Identifier = NULL");

					foreach (string name in nameList) {
						sb.Append (" OR ");

						sb.Append (string.Format ("Identifier like '{0}'", name));
					}

					sb.Append (")");
				}
			}

			var conn = ORM.CreateConnection ();
			JSONObject resp = null;
			try
			{
				conn.Open ();

				resp = new SettingsReturn(ORM.Select<Configuration> ("Configuration", sb.ToString (), ref conn));
				_parseSuccess = true;
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

		public override System.Collections.Generic.List<QueryKeyInfo> QueryKeyDetails {
			get {
				List<QueryKeyInfo> _keyDetails = new List<QueryKeyInfo> ();

				//_keyDetails.Add (new QueryKeyInfo ("key", "One or more configuration types in addition to global (you can use '%' as a wildcard in the name)", typeof(string), false));
				_keyDetails.Add (new QueryKeyInfo ("key", "One or more configuration keys (you can use '%' as a wildcard in the name)", typeof(string), false));
				_keyDetails.Add (new QueryKeyInfo ("id", "One or more parent identifiers in addition to the default value (if none are specified, all types will be queried)", typeof(string), false));

				return _keyDetails;
			}
		}

		#endregion
	}
}

