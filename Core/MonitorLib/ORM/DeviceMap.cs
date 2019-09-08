using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;

namespace Monitoring
{

	public class DeviceMapMapper : ClassMapper<DeviceMap>
	{
		public DeviceMapMapper()
		{
			Table ("DeviceMap");
			Map (m => m.DeviceTypeEnum).Ignore ();
			AutoMap ();
		}
	}

	public class DeviceMap : PersistableObject
	{
		public DeviceMap () : base()
		{
		}

		#region implemented abstract members of PersistableObject

		public override void GenerateKeys ()
		{
			//IDGuid = Guid.NewGuid ();
		}

		#endregion

		public int ID {get; set;}
		public string Name {get;set;}
		public string DeviceKey {get;set;}
		public int DeviceType {get;set;}
		public DateTimeStamp Created {get;set;}
		public int ParentMapID {get; set; }


		public DeviceType DeviceTypeEnum 
		{
			get
			{
				return (DeviceType)DeviceType;
			}
			set
			{
				DeviceType = (int)value;
			}
		}

		public static DeviceMap Create (string deviceKey, DeviceType type)
		{
			var match = ORM.Select<DeviceMap> ("DeviceMap", string.Format ("DeviceKey = '{0}' and DeviceType = ", deviceKey, (int)type)).FirstOrDefault ();
			if (match != null) {
				return match;
			} else {
				match = new DeviceMap ();
				match.Created = DateTimeStamp.Now;
				match.DeviceKey = deviceKey;
				match.DeviceTypeEnum = type;
				match.Name = deviceKey;

				ORM.Update<DeviceMap> ("DeviceMap", match);
				match = ORM.Select<DeviceMap> ("DeviceMap", string.Format ("DeviceKey = '{0}' and DeviceType = ", deviceKey, (int)type)).FirstOrDefault ();

				return match;
			}
		}

		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> MapLogs(IEnumerable<DeviceMap> maps, IEnumerable<DeviceLog> logs)
		{
			Dictionary<DeviceMap, IEnumerable<DeviceLog>> retLogs = new Dictionary<DeviceMap, IEnumerable<DeviceLog>> ();

			foreach (Tuple<DeviceMap, List<DeviceLog>> tuple 
				in maps.Distinct<DeviceMap>().Select (
					m => new Tuple<DeviceMap, List<DeviceLog>> (m, logs.Where (l => l.DeviceMapID == m.ID).ToList ()))) {
				retLogs.Add (tuple.Item1, tuple.Item2);
			}

			return retLogs;
		}

		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> GetLogs(IEnumerable<Guid> mapIDs)
		{
			return GetLogs (null, null, mapIDs);
		}
		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> GetLogs(DateTime? startTime, DateTime? endTime)
		{
			return GetLogs (startTime, endTime, null);
		}
		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> GetLogs(Guid mapID)
		{
			return GetLogs (null, null, mapID);
		}
		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> GetLogs(DateTime? startTime, DateTime? endTime, Guid mapID)
		{
			List<Guid> mapIDs = new List<Guid> ();
			mapIDs.Add (mapID);

			return GetLogs (startTime, endTime, mapIDs);
		}
		public static Dictionary<DeviceMap, IEnumerable<DeviceLog>> GetLogs(DateTime? startTime, DateTime? endTime, IEnumerable<Guid> mapIDs)
		{
			StringBuilder sbFilter = new StringBuilder ();
			StringBuilder inBuilder = null;

			if (startTime != null && startTime > DateTime.MinValue) {
				sbFilter.Append ("TimeStamp >= '");
				sbFilter.Append (startTime.ToString());
				sbFilter.Append ("' ");
			}

			if (endTime != null && endTime > DateTime.MinValue) {
				if (sbFilter.Length > 0) {
					sbFilter.Append ("AND ");
				}
				sbFilter.Append ("TimeStamp <= '");
				sbFilter.Append (endTime.ToString());
				sbFilter.Append ("' ");
			}

			if (mapIDs != null && mapIDs.Count() > 0) {
				if (sbFilter.Length > 0) {
					sbFilter.Append ("AND ");
				}

				inBuilder = new StringBuilder ();
				foreach (var id in mapIDs) {
					if (inBuilder.Length > 0) {
						inBuilder.Append (", ");
					}
					inBuilder.Append("'");
					inBuilder.Append(id.ToString());
					inBuilder.Append("'");
				}

				sbFilter.Append ("DeviceMapID IN (");
				sbFilter.Append (inBuilder.ToString());
				sbFilter.Append (")");
			}

			var tmpLogs = ORM.Select<DeviceLog> ("DeviceLog", sbFilter.ToString ());

			inBuilder = new StringBuilder ();
			foreach (int id in tmpLogs.Select(l => l.DeviceMapID).Distinct()) {
				if (inBuilder.Length > 0) {
					inBuilder.Append (", ");
				}
				inBuilder.Append("'");
				inBuilder.Append(id.ToString());
				inBuilder.Append("'");
			}

			var tmpMaps = ORM.Select<DeviceMap> ("DeviceMap", string.Format ("ID in ({0})", inBuilder.ToString ()));

			return MapLogs (tmpMaps, tmpLogs);
		}

		public bool LogValue(IConvertible Value, DateTime logTimeStamp)
		{
			DeviceLog log = new DeviceLog ();
			log.DeviceMapID = this.ID;
			log.TimeStamp = new DateTimeStamp(logTimeStamp);
			log.Value = Value.ToString ();

			return ORM.Update<DeviceLog> ("DeviceLog", log);
		}
	}

	public enum DeviceType
	{
		Temperature,
		Humidity,
		Barometer,
		PH,
		O2,
		Webcam,
		FreeGigabytes
	}

	public class DeviceResult
	{
		public DeviceResult(DeviceMap map, IEnumerable<DeviceLog> logs) 
		{
			Map = map;
			LogEntries = new List<DeviceLog>(logs);
		}

		public DeviceMap Map {get;set;}
		public List<DeviceLog> LogEntries {get;set;}
	}
	public class DeviceResults
	{
		private List<DeviceResult> _cachedResults = null;

		public List<DeviceMap> Maps { get; set; }
		public List<DeviceLog> Logs {get;set;}

		public List<DeviceResult> Results{ 
			get 
			{
				if (_cachedResults == null) {
					_cachedResults = Maps.Select (m => new DeviceResult (m, Logs.Where(l => l.DeviceMapID == m.ID).AsEnumerable())).ToList();
				}
				return _cachedResults;
			}
		}

		public static DeviceResults Query (DateTime startDate, DateTime endDate, List<string> deviceKeys)
		{
			StringBuilder queryBldr = new StringBuilder ();
			DeviceResults returnResults = new DeviceResults ();

			queryBldr.Append (string.Format ("DeviceType = {0} ", (int)DeviceType.Temperature));
			if (deviceKeys.Count > 0)
			{
				bool first = true;
				queryBldr.Append ("AND DeviceKey IN (");
				foreach (var key in deviceKeys) {

					if (!first)
						queryBldr.Append (",");
					else
						first = false;

					queryBldr.Append ("'");
					queryBldr.Append (key);
					queryBldr.Append ("'");
				}
				queryBldr.Append(")");
			}

			returnResults.Maps = (ORM.Select<DeviceMap>("DeviceMap",queryBldr.ToString())).ToList();

			returnResults.Logs = (ORM.Select<DeviceLog>("DeviceLog",string.Format("DeviceMapID in (select ID from DeviceMap where {0})",queryBldr.ToString()))).ToList();

			return returnResults;
		}
	}
}

