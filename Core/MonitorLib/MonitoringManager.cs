using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Monitoring
{
	public static class MonitoringManager
	{
		private static int _intervalSeconds = 600;
		private static DateTime _lastUpdate = DateTime.MinValue;
		private static List<ISensorDiscovery> _sensorDiscoveryDevices = null;
		private static List<ILoggableConnection> _connections = new List<ILoggableConnection>();
		private static Dictionary<string, IConvertible> _lastValues = new Dictionary<string, IConvertible>();
		private static Thread _logThread = null;
		private static bool _runLogging = true;

		//private static MonitorWebsite upnpSite = new MonitorWebsite();

		static MonitoringManager()
		{

			StatusLED.SetColor (true, false, true);
#if DEBUG
			_intervalSeconds = 10;
#endif

			if (_lastUpdate == DateTime.MinValue || DateTime.Now.Subtract(_lastUpdate).TotalSeconds > _intervalSeconds) {
				//TODO: Update code for monitoring manager.
			}
			UpdateConnections ();
			BeginLogging ();

			StatusLED.SetColor (false, false, true);
		}

		private static void BeginLogging()
		{
			if (_logThread != null) {
				_logThread.Abort ();
				_logThread = null;
			}

			_logThread = new Thread (new ThreadStart (LogLoop));
			_logThread.Start ();
		}

		private static void LogLoop()
		{
			DateTime lastRun = DateTime.Now.Subtract(TimeSpan.FromSeconds(_intervalSeconds + 1));
			while (_runLogging) {
				if (DateTime.Now.Subtract(lastRun).TotalSeconds > _intervalSeconds)
				{
					//StatusLED.SetColor (false, true, false);
					lastRun = DateTime.Now;

//					try
//					{
//						UpdateConnections();
//					}
//					catch
//					{
//					}

					if (_connections != null) {

						var loggableConnections = _connections.Where (c => (c.IsChartable || c.IsInDashboard) && !c.IsJpegFrame);
						LogWriter.WriteLog (string.Format ("Logging values for {0} devices.", loggableConnections.Count()));
						Type convertibleType = typeof(LoggableConnection<IConvertible>);

						foreach (ILoggableConnection conn in loggableConnections) {
							LogWriter.WriteLog (string.Format ("Logging value for device {1} with key {0}.", conn.Key, conn.GetType().FullName));
							try {
								IConvertible val = conn.GetAndLogValue (lastRun);

								//Add the value to the lastValues dictionary
								if (!_lastValues.ContainsKey (conn.Map.DeviceKey))
									_lastValues.Add (conn.Map.DeviceKey, val);
								else
									_lastValues [conn.Map.DeviceKey] = val;
							} catch (Exception ex) {
								LogWriter.WriteLog (string.Format ("An error was encountered attempting to log a {1} connection for the key {0}", conn.Key, conn.GetType ().FullName), ex);
							}
						}


						StatusLED.SetColor (false, false, true);
					}
					//Calculate the time to sleep this thread until the next run.
					TimeSpan sleep = TimeSpan.FromSeconds (_intervalSeconds - 1);
					if (DateTime.Now.Subtract (lastRun).TotalSeconds <= sleep.TotalSeconds)
						sleep = sleep.Subtract(DateTime.Now.Subtract (lastRun));
					else
						sleep = TimeSpan.FromSeconds (1);

					Thread.Sleep(sleep);
				}
			}
		}

		public static DateTime LastUpdated
		{
			get {
				return _lastUpdate;
			}
		}

		public static Dictionary<string,IConvertible> LastValues
		{
			get {
				return _lastValues;
			}
		}

		private static void UpdateConnections()
		{
			if (_sensorDiscoveryDevices == null) {
				_sensorDiscoveryDevices = new List<ISensorDiscovery> ();
				IEnumerable<ISensorDiscovery> newDiscoverers = typeof(MonitoringManager).Assembly.GetTypes ()
					.Where (t => !t.IsAbstract && !t.IsGenericType && !t.IsInterface &&
                           typeof(ISensorDiscovery).IsAssignableFrom (t))
					.Select (t => (ISensorDiscovery)
						typeof(MonitoringManager).Assembly.CreateInstance (t.FullName, true));


					_sensorDiscoveryDevices.AddRange(newDiscoverers);
			}

			foreach (ISensorDiscovery disc in _sensorDiscoveryDevices) {
				LogWriter.WriteLog (string.Format ("Discovering available devices for monitor {0}", disc.GetType ().FullName));
				try
				{
					foreach(string key in disc.ListDevices().Where(d => d != null && !_connections.Any(c => c.Key.Trim().ToUpper() == d.Trim().ToUpper())))
					{
						LogWriter.WriteLog (string.Format ("Attempting to add monitor for {1} using discoverer {0}", disc.GetType ().FullName, key));
						try
						{
							var con = disc.CreateConnection(key);
							if (con != null)
								_connections.Add(con);
						}
						catch(Exception ex) {
							LogWriter.WriteLog (string.Format ("Error while attempting to add connection for device {0}", key), ex);
						}
					}
				}
				catch(Exception ex) {
					LogWriter.WriteLog (string.Format ("Error while attempting to list devices for {0}", disc.GetType().FullName), ex);
				}
			}
		}

		public static IEnumerable<string> ChartDeviceKeys
		{
			get {
				return _connections.Where(c => c.IsChartable).Select (c => c.Key).AsEnumerable ();
			}
		}

		public static IEnumerable<string> DashboardDeviceKeys
		{
			get {
				return _connections.Where(c => c.IsInDashboard).Select (c => c.Key).AsEnumerable ();
			}
		}

		public static IEnumerable<string> DeviceKeys
		{
			get {
				return _connections.Select (c => c.Key).AsEnumerable ();
			}
		}

		public static IConvertible ReadDevice (string deviceKey)
		{
			var device = _connections.FirstOrDefault (d => d.Key == deviceKey);
			if (device != null) {
				return device.GetValue ();
			}
			return null;
		}

		public static IConvertible Min(string deviceKey)
		{
			var device = _connections.FirstOrDefault (d => d.Key == deviceKey);
			return device.Min;
		}
		public static IConvertible Max(string deviceKey)
		{
			var device = _connections.FirstOrDefault (d => d.Key == deviceKey);
			return device.Max;
		}

		public static string Name (string deviceKey)
		{
			var device = _connections.FirstOrDefault (d => d.Key == deviceKey);
			return device.Name;
		}
		public static string Suffix (string deviceKey)
		{
			var device = _connections.FirstOrDefault (d => d.Key == deviceKey);
			return device.Suffix;
		}
	}
}

