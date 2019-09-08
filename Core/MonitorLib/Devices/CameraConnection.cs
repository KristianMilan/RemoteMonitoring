using System;
using RaspberryCam;
using RaspberryCam.Servers;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public class CameraDiscoverer : ISensorDiscovery
	{
		private static Dictionary<string, CameraConnection> _connections = new Dictionary<string, CameraConnection>();
		private static Dictionary<string, int> _ports = new Dictionary<string, int>();

		static int nextPort = 8101;

		#region ISensorDiscovery implementation
		public IEnumerable<string> ListDevices ()
		{
			List<string> Devices = new List<string> ();
			try
			{
				DirectoryInfo devDir = new DirectoryInfo ("/dev");
				if (devDir.Exists) {
					foreach (var file in devDir.GetFiles("video*").Where(f => !Devices.Contains(f.FullName))) {
						Devices.Add (file.FullName);
					}
				}
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("An error was encountered while attempting to list devices.", ex);
			}
			return Devices;
		}

		public ILoggableConnection CreateConnection (string key)
		{
			try
			{
				lock(_connections)
				{
					if (_connections.ContainsKey(key))
					{
						return _connections[key];
					}
				}

				lock(_ports)
				{
					FileInfo file = new FileInfo (key);
					if (!file.Exists) {
						DirectoryInfo devDir = new DirectoryInfo ("/dev");
						file = devDir.GetFiles (key).FirstOrDefault ();
					}

					if (file.Exists) {

						lock(_connections)
						{
							if (!_ports.ContainsKey (key)) {
								_connections.Add (key, new CameraConnection (key, file.FullName, nextPort));
		//						CameraConnection connection = new CameraConnection(key, file.FullName, nextPort);
		//						_connections.Add(key, new Tuple<string, int>(file.FullName, connection.Port));
								_ports.Add (key, nextPort);
								nextPort++;

							}
							return _connections [key];
						}
					}
				}
			}
			catch(Exception ex) {
				LogWriter.WriteLog (string.Format ("An error was encountered while attempting to start video device {0}.", key), ex);
			}
			return null;
		}
		#endregion
	}
	public class CameraConnection : LoggableConnection<string>
	{
		private TcpVideoServer _server;
		private int _port;
		private Cameras _cam;
		private string _key;
		private string _path;

		public CameraConnection ()
		{

		}

		public CameraConnection(string key, string path, int port)
		{
			try
			{
				_key = key;
				_path = path;
				_port = port;
				_cam = Cameras.DeclareDevice ().Named (key).WithDevicePath (path).Memorize ();
				//_server = new TcpVideoServer (_port, _cam);
			}
			catch(Exception ex) {
				LogWriter.WriteLog(string.Format("An error occurred while attempting to initialize the camera {0}", key), ex);
				throw ex;
			}
		}

		public int Port
		{
			get {
				return _port;
			}
		}

		public void StartServer()
		{
			try
			{
				//_server.Start ();
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("An error was encountered while attempting to start video server.", ex);
			}
		}
		public void StopServer(string cameraPath)
		{
			try
			{
				//_server.Stop ();
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("An error was encountered while attempting to start video server.", ex);
			}
		}

		public string CameraPath
		{
			get
			{
				return _path;
			}
		}

		public override IEnumerable<DeviceLog> GetLog (DateTime startTime, DateTime endTime)
		{
			//TODO: Implement screenshot grabbing for the log.
			//Return an empty device log for now
			return new List<DeviceLog>();
		}

		public override string ReadAndLogValue (DateTime logTimeStamp)
		{
			//TODO: Implement screenshot grabbing for the log.
			return ReadValue ();
		}

		#region implemented abstract members of LoggableConnection

		public override string ReadValue ()
		{
			if (_cam != null) {
				try
				{
					var img = _cam.Default.TakePicture(new PictureSize(), new Percent(90));
					return Convert.ToBase64String(img);
				}
				catch(Exception ex) {
					LogWriter.WriteLog (string.Format ("An error was encountered while trying to get a frame from camera {0}:{1}", Map.Name, Map.DeviceKey), ex);
				}
			}
			return null;
		}

		public override string Key {
			get {
				return Map.DeviceKey;
			}
		}

		public override DeviceType DeviceType {
			get {
				return DeviceType.Webcam;
			}
		}

		public override IConvertible Max {
			get {
				return 1;
			}
		}

		public override IConvertible Min {
			get {
				return 0;
			}
		}

		public override string Name {
			get {
				return Map.Name;
			}
		}

		public override string Suffix {
			get {
				return "Image";
			}
		}

		public override bool IsChartable {
			get {
				return false;
			}
		}

		public override bool IsInDashboard {
			get {
				return false;
			}
		}
			
		public override bool IsJpegFrame {get{ return true; }}
		#endregion
	}
}

