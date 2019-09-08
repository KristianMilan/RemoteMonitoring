using System;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public class EditMapJSONResponse : JSONObject
	{
		public EditMapJSONResponse() : base()
		{
		}
		public EditMapJSONResponse (DeviceMap map, bool success) : this ()
		{
			Value = map;
			SuccessfullyUpdated = success;
		}

		public DeviceMap Value {get;set;}
		public bool SuccessfullyUpdated {get;set;}
	}
	public class EditDeviceMap : JSONRequestImplementation<EditMapJSONResponse>
	{
		public EditDeviceMap ()
		{
		}

		#region implemented abstract members of JSONRequestImplementation

		public override EditMapJSONResponse GetResponse (Dictionary<string, string[]> requestAttributes)
		{
			string mapIDstr = string.Empty;
			try
			{
				if (requestAttributes.ContainsKey ("ID")) {
					mapIDstr = requestAttributes ["ID"] [0].Trim ().ToUpper ();

					var map = ORM.Select<DeviceMap>("DeviceMap",string.Format("ID = '{0}'",mapIDstr)).FirstOrDefault();

					if (map == null)
						throw new KeyNotFoundException(string.Format("The map with an ID of {0} does not exist in the database.", mapIDstr));


					foreach(string key in requestAttributes.Keys)
					{
						switch(key.Trim().ToUpper())
						{
						case "NAME":
							map.Name = requestAttributes[key][0];
							break;
						case "DEVICEKEY":
							map.DeviceKey = requestAttributes[key][0];
							break;
						case "DEVICETYPE":
							map.DeviceTypeEnum = (DeviceType)Enum.Parse(typeof(DeviceType), requestAttributes[key][0]);
							break;
						case "CREATED":
							map.Created = new DateTimeStamp(DateTime.Parse(requestAttributes[key][0]));
							break;
						case "PARENTMAPID":
							map.ParentMapID = (int)int.Parse(requestAttributes[key][0]);
							break;
						}
					}

					bool success = ORM.Update<DeviceMap>("DeviceMap",map);
					return new EditMapJSONResponse(map, success);

				}
			}
			catch(Exception ex) {
				if (string.IsNullOrEmpty (mapIDstr))
					mapIDstr = "NULL";

				LogWriter.WriteLog (string.Format ("An error was encountered while attempting to update the map. ID:{0}", mapIDstr), ex);
			}
				
			return new EditMapJSONResponse (null, false);
		}

		public override System.Collections.Generic.List<QueryKeyInfo> QueryKeyDetails {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}
}

