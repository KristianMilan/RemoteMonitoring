using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Monitoring
{
	public class FreeSpaceDiscoverer : ISensorDiscovery
	{
		private static List<DriveInfo> _drives = null;

		static FreeSpaceDiscoverer()
		{
			_drives = DriveInfo.GetDrives ().ToList ();
		}

		#region ISensorDiscovery implementation
		public System.Collections.Generic.IEnumerable<string> ListDevices ()
		{
			return _drives.Select (d => d.RootDirectory.FullName).AsEnumerable ();
		}
		public ILoggableConnection CreateConnection (string key)
		{
			DriveInfo drive = _drives.FirstOrDefault (d => d.RootDirectory.FullName.Trim ().ToUpper () == key.Trim ().ToUpper ());
			if (drive != null)
				return new FreeSpaceConnection (drive);
			else
				return null;
		}
		#endregion
	}

	public class FreeSpaceConnection : LoggableConnection<Double>
	{
		private DriveInfo _drive = null;

		public FreeSpaceConnection () : base()
		{
			_drive = DriveInfo.GetDrives().First();
		}
		public FreeSpaceConnection (string path)
		{
			_drive = DriveInfo.GetDrives ().FirstOrDefault (d => d.RootDirectory.FullName.Trim ().ToUpper () == Key.Trim ().ToUpper ());
		}
		public FreeSpaceConnection (DriveInfo drive)
		{
			_drive = drive;
		}


		#region implemented abstract members of LoggableConnection
		private double _lastValue = -1f;
		private double _maxSize = 8192f;
		public override double ReadValue ()
		{
			try
			{
			if (_drive != null)
				{
					_lastValue = Math.Round((_drive.TotalSize - _drive.TotalFreeSpace) / 1024f / 1024f / 1024f, 3);
				   	_maxSize = Math.Round (_drive.TotalSize / 1024f / 1024f / 1024f, 3);
				}
			else
				_lastValue = -1f;
			}
			catch {
			}
			return _lastValue;
		}
		public override string Key {
			get {
				if (_drive != null)
					return _drive.RootDirectory.FullName;
				else
					return null;
			}
		}
		public override DeviceType DeviceType {
			get {
				return DeviceType.FreeGigabytes;
			}
		}

		public override IConvertible Max {
			get {
				return _maxSize;
			}
		}

		public override IConvertible Min {
			get {
				return 0;
			}
		}

		public override string Name {
			get {
				return string.Format("Used space on drive {0}", Key);
			}
		}

		public override string Suffix {
			get {
				return "GB";
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

