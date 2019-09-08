using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Monitoring
{
	/// <summary>
	/// Chart JS graph dev response.
	/// <example>
	/// 
	///var data = {
	///	labels: [
	///		"03/09/2015 11:00:00", 
	///		"03/09/2015 11:10:00", 
	///		"03/09/2015 11:20:00", 
	///		"03/09/2015 11:30:00", 
	///		"03/09/2015 11:40:00", 
	///		"03/09/2015 11:50:00", 
	///		"03/09/2015 12:00:00", 
	///		"03/09/2015 12:10:00", 
	///		"03/09/2015 12:20:00", 
	///		"03/09/2015 12:30:00", 
	///		"03/09/2015 12:40:00", 
	///		"03/09/2015 12:50:00", 
	///		"03/09/2015 13:00:00",
	///		"03/09/2015 13:10:00"
	///	],
	///	datasets: [
	///		{
	///			label: "Device 1",
	///			fillColor: "rgba(100,0,0,0.1)",
	///			strokeColor: "rgba(100,0,0,0.6)",
	///			pointColor: "rgba(100,0,0,0.9)",
	///			data: [32,32,33,33,34,34,35,36,36,37,38,39,40,41]
	///		},
	///		{
	///			label: "Device 2",
	///			fillColor: "rgba(200,0,0,0.1)",
	///			strokeColor: "rgba(200,0,0,0.6)",
	///			pointColor: "rgba(200,0,0,0.9)",
	///			data: [36,36,37,38,39,40,42,44,48,52,55,58,62,67]
	///		},
	///		{
	///			label: "Device 3",
	///			fillColor: "rgba(200,100,0,0.1)",
	///			strokeColor: "rgba(200,100,0,0.6)",
	///			pointColor: "rgba(200,100,0,0.9)",
	///			data: [35,36,37,38,40,42,44,46,48,51,54,57,60,64]
	///		}
	///	]
	///};
	/// </example>
	/// </summary>
	public class ChartJSGraphDevResponse : DeviceLogJSONResponse
	{
		public ChartJSGraphDevResponse () : base ()
		{
		}

		public ChartJSGraphDevResponse(IEnumerable<DeviceLog> deviceLog, IEnumerable<DeviceMap> maps) :base(deviceLog, maps)
		{
		}

		public override string ToJSON ()
		{
			MemoryStream str = new MemoryStream ();
			StreamWriter tw = new StreamWriter (str);
			JsonTextWriter w = new JsonTextWriter (tw);
			StreamReader rdr;

			List<string> dateResult = Values.SelectMany (v => v.Value.Select (v2 => v2.TimeStamp.ToString())).ToList();
			dateResult = dateResult.OrderBy (d => d).Distinct().ToList ();

			w.WriteStartObject ();
				w.WritePropertyName("labels");
				w.WriteStartArray ();

				foreach (string dateVal in dateResult) {
					DateTimeStamp dtstamp = DateTimeStamp.Parse (dateVal);
				w.WriteValue(string.Format("{0}", dtstamp.ToStandardString()));
				}

				w.WriteEndArray ();



				w.WritePropertyName ("datasets");
				w.WriteStartArray ();

				int colorIncrementer = 0;
				foreach (var key in Values.Keys) {
					w.WriteStartObject ();

						w.WritePropertyName("label");
						w.WriteValue(string.Format("{0}",key.Name));

						w.WritePropertyName("fillColor");
						w.WriteValue(GetArgb(colorIncrementer, 0.2f));

						w.WritePropertyName("strokeColor");
						w.WriteValue(GetArgb(colorIncrementer, 0.7f));

						w.WritePropertyName("pointColor");
						w.WriteValue(GetArgb(colorIncrementer, 1.0f));

						w.WritePropertyName("data");

						w.WriteStartArray();
						foreach(string dt in dateResult)
							{
								var DevLog = Values[key].Where(v => v.TimeStamp.ToString() == dt).FirstOrDefault();
								string value = null;
								if (DevLog != null)
									value = DevLog.Value;

								w.WriteValue(value);
							}
						w.WriteEndArray();


					w.WriteEndObject();

					colorIncrementer ++;
				}
				w.WriteEndArray();
			w.WriteEndObject ();

			w.Flush ();
			str.Flush ();

			str.Position = 0;

			rdr = new StreamReader (str);
			return rdr.ReadToEnd ();
		}

		private string GetArgb(int incrementer, float opacity)
		{
			int blueVal= incrementer % 3 * 100 + (incrementer * 10);
			int greenVal = incrementer % 4 * 100 + (incrementer * 20);
			int redVal = incrementer % 5 * 100 + (incrementer * 30);

			while (redVal > 255)
				redVal -= 255;
			while (greenVal > 255)
				greenVal -= 255;
			while (blueVal > 255)
				blueVal -= 255;

			return string.Format ("rgba({0},{1},{2},{3})", redVal, greenVal, blueVal, opacity);
		}
	}
}

