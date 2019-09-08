using System;
using Raspberry;
using Raspberry.Timers;
using Raspberry.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Monitoring
{
	public class DS18B20Discoverer : ISensorDiscovery
	{
		public IEnumerable<string> ListDevices()
		{
			try
			{
				DirectoryInfo OneWireDir = (new DirectoryInfo (Constants.ONE_WIRE_DIRECTORY));
				if (OneWireDir.Exists) {
					var retVal = OneWireDir.GetDirectories("28*").Where(d => d.Exists && d.GetFiles().Any(f => f.Name.Trim().ToUpper() == "W1_SLAVE")).Select(d => d.FullName).ToList();
					LogWriter.WriteLog (string.Format ("Found {1} OneWire device directories while enumerating through {0}.", Constants.ONE_WIRE_DIRECTORY, retVal.Count));
					//IEnumerable<string> files = OneWireDir.EnumerateDirectories ("28*").Where (d => d.Exists).Select (f => f.FullName).Where (s => !string.IsNullOrEmpty (s)).ToList ();
					return retVal;
				} else {
					LogWriter.WriteLog (string.Format("One wire directory {0} was not found.  DS18B20 Temperature sensors will not be available.", Constants.ONE_WIRE_DIRECTORY));
					return new List<string> ().AsEnumerable();
				}
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("Error encountered trying to locate DS18B20 devices.", ex);
			}
			return new List<string> ();
		}

		public ILoggableConnection CreateConnection (string key)
		{
			return new DS18B20Connection (key);
		}
	}
	public class DS18B20Connection : TemperatureConnectionBase
	{
		private string _devicePath;
		public DS18B20Connection () : base()
		{
		}
		public DS18B20Connection (string deviceFilePath) : this()
		{
			LogWriter.WriteLog (string.Format("Initializing object for DS18B20 device at {0}.", deviceFilePath));
			IsCelcius = false;
			_devicePath = deviceFilePath;
		}

		#region implemented abstract members of LoggableDevice

		public override double ReadValue ()
		{
			try
			{
				LogWriter.WriteLog (string.Format("Reading value for DS18B20 device at {0}.", _devicePath));
				DirectoryInfo deviceDir = new DirectoryInfo (_devicePath);
				FileInfo wSlaveFile = deviceDir.GetFiles ("w1_slave").FirstOrDefault ();
				if (wSlaveFile != null && wSlaveFile.Exists) {
					LogWriter.WriteLog (string.Format("Found OneWire file {0}.", wSlaveFile.FullName));
					var reader = new StreamReader(wSlaveFile.OpenRead());
					string val = reader.ReadToEnd ();
					reader.Close();
					LogWriter.WriteLog(string.Format("Device file read complete: {0}", val));

					string[] valarray = val.Split(new string[]{"t="}, StringSplitOptions.RemoveEmptyEntries);
					if (valarray.Count() > 1)
					{
						Double dVal = int.Parse(valarray[1]) / 1000f;
						LogWriter.WriteLog(string.Format("Temperature Device {0} reads {3} -  {1} C / {2} F", wSlaveFile.Name, dVal, ConvertFromC(dVal), valarray[1]));
						return ConvertFromC(dVal);
					}
					else
					{
						LogWriter.WriteLog(string.Format("Value for {0} was not found in the file.", wSlaveFile.Name));
					}
				}
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("Error encountered trying to locate DS18B20 devices.", ex);
			}
			return (double)Min;
		}


		public override string Key
		{
			get {
				return _devicePath;
			}
		}

		#endregion

		#region implemented abstract members of TemperatureMonitorBase

		public override bool IsCelcius {
			get;
			set;
		}

		#endregion

		#region implemented abstract members of LoggableConnection

		public override IConvertible Max {
			get {
				return 115;
			}
		}

		public override IConvertible Min {
			get {
				return -25;
			}
		}

		public override string Suffix {
			get {
				return "Fahrenheit";
			}
		}

		public override string Name {
			get {
				string identifier = Map.Name;
				if (string.IsNullOrEmpty (identifier))
					identifier = Key;
				return string.Format("Temperature for {0}", identifier);
			}
		}

		public override bool IsChartable {
			get {
				return true;
			}
		}

		public override bool IsInDashboard {
			get {
				return true;
			}
		}

		#endregion
	}
}

