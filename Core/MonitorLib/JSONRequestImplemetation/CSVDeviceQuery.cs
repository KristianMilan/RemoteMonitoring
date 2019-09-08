using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monitoring
{
	public class CSVDeviceResponse : DeviceLogJSONResponse
	{
		public CSVDeviceResponse ()
		{
		}
		public CSVDeviceResponse(IEnumerable<DeviceLog> deviceLog, IEnumerable<DeviceMap> maps) :base(deviceLog, maps)
		{
		}

		public override string ContentType
		{
			get {
				return "text/csv";
			}
		}
		public override string Extension
		{
			get
			{
				return "csv";
			}
		}

		public override string ToJSON ()
		{
			MemoryStream respStream = new MemoryStream ();
			StreamWriter w = new StreamWriter (respStream);
			StreamReader r;
			StringBuilder lineBuilder;

			Dictionary<string, List<string>> respValues = new Dictionary<string, List<string>> ();

			List<string> dateResult = Values.SelectMany (v => v.Value.Select (v2 => v2.TimeStamp.ToString())).ToList();
			dateResult = dateResult.OrderBy (d => d).Distinct().ToList ();

			foreach (string dateVal in dateResult) {
				List<string> vals = new List<string> ();
				//vals.Add (dateVal);
				foreach (var Key in Values.Keys) {
					var logVal = Values [Key].Where (v => v.TimeStamp.ToString() == dateVal).FirstOrDefault ();
					if (logVal != null) {
						vals.Add (logVal.Value);
					}
					else
					{
						vals.Add(string.Empty);
					}
				}

				respValues.Add(dateVal, vals);
			}

			lineBuilder = new StringBuilder();
			lineBuilder.Append ("TimeStamp");
			foreach(var Key in Values.Keys)
			{
				if (lineBuilder.Length > 0)
					lineBuilder.Append(",");

				lineBuilder.Append(string.Format("'{0} ({1})'",Key.Name, Key.DeviceKey));
			}
			w.WriteLine(lineBuilder.ToString());

			foreach(string key in respValues.Keys)
			{
				lineBuilder = new StringBuilder();
				lineBuilder.Append (key);
				foreach(var val in respValues[key])
				{
					if (lineBuilder.Length > 0)
						lineBuilder.Append(",");

					lineBuilder.Append(val);
				}
				w.WriteLine(lineBuilder.ToString());
			}

			w.Flush();
			respStream.Flush();
			respStream.Position = 0;

			r = new StreamReader(respStream);

			return r.ReadToEnd();
		}
	}
}

