using System;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public class DeviceListResponse : JSONObject, IEnumerable<DeviceInfoJSON>
	{
		private IEnumerable<DeviceInfoJSON> _maps;

		public DeviceListResponse()
		{
			_maps = new List<DeviceInfoJSON> ();
		}
		public DeviceListResponse(IEnumerable<DeviceInfoJSON> maps)
		{
			_maps = maps;
		}
		#region IEnumerable implementation

		public IEnumerator<DeviceInfoJSON> GetEnumerator ()
		{
			return _maps.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return _maps.GetEnumerator ();
		}

		#endregion


	}

	public class DeviceInfoJSON : JSONObject
	{
		public DeviceInfoJSON()
		{
		}
		public DeviceInfoJSON (DeviceMap map) : this ()
		{
			ID = map.ID;
			Name = map.Name;
			Key = map.DeviceKey;
			DeviceType = map.DeviceTypeEnum.ToString ();
			Parent = map.ParentMapID;
		}
		public int ID{get;set;}
		public string Name{get;set;}
		public string Key{ get; set; }
		public string DeviceType{ get; set; }
		public int Parent {get;set;}
	}

	public class ListDevices : JSONRequestImplementation<DeviceListResponse>
	{
		public ListDevices ()
		{

		}

		#region implemented abstract members of JSONRequestImplementation

		public override DeviceListResponse GetResponse (Dictionary<string, string[]> requestAttributes)
		{
			return new DeviceListResponse (ORM.Select<DeviceMap> ("DeviceMap", "").Select (d => new DeviceInfoJSON (d)).ToList ());
		}

		public override List<QueryKeyInfo> QueryKeyDetails {
			get {
				return new List<QueryKeyInfo> ();
			}
		}

		#endregion
	}
}

