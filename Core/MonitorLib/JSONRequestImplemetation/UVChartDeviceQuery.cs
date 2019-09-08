using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Monitoring
{
	public class UVChartGraphDevResponse : DeviceLogJSONResponse
	{
		public UVChartGraphDevResponse () : base ()
		{
		}

		public UVChartGraphDevResponse(IEnumerable<DeviceLog> deviceLog, IEnumerable<DeviceMap> maps) :base(deviceLog, maps)
		{
		}

		public override string ToJSON ()
		{
			MemoryStream str = new MemoryStream ();
			StreamWriter tw = new StreamWriter (str);
			JsonTextWriter w = new JsonTextWriter (tw);
			StreamReader rdr;

			w.WriteStartObject ();
			w.WritePropertyName("categories");
			w.WriteStartArray ();

			foreach (var key in Values.Keys) {
				w.WriteValue(string.Format("{0} ({1})", key.Name, key.DeviceKey));
			}

			w.WriteEndArray ();
			w.WriteEndObject ();



			w.WriteStartConstructor ("dataset");
			w.WriteStartArray ();

			foreach (var key in Values.Keys) {
				w.WriteStartConstructor(string.Format("{0} ({1})", key.Name, key.DeviceKey));
				w.WriteStartArray();

				foreach(var val in Values[key])
				{
					w.WriteStartObject ();
					w.WritePropertyName("name");
					w.WriteValue(val.TimeStamp);
					w.WriteEndObject();

					w.WriteStartObject ();
					w.WritePropertyName("value");
					w.WriteValue(val.Value);
					w.WriteEndObject();
				}

				w.WriteEndArray();
				w.WriteEndConstructor();
			}

			w.WriteEnd();
			w.WriteEndConstructor ();

			w.Flush ();
			str.Flush ();

			str.Position = 0;

			rdr = new StreamReader (str);
			return rdr.ReadToEnd ();
		}
	}
}

