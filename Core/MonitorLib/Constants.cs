using System;
using System.IO;

namespace Monitoring
{
	public static class Constants
	{
		public static string ONE_WIRE_DIRECTORY
		{
			get {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Unix:
					if (Directory.Exists ("/Applications") &
					    Directory.Exists ("/System") &
					    Directory.Exists ("/Users") &
						Directory.Exists ("/Volumes")) {
						//TODO: verify this is correct for Mac OS X
						return "/sys/bus/w1/devices";
					} else {
						//Linux
						return "/sys/bus/w1/devices";
					}
				case PlatformID.MacOSX:
					//TODO: verify this is correct for Mac OS X
					return "/sys/bus/w1/devices";
				default: //Windows
					return @"c:\Program Files\OneWire";
				}
			}
		}
	}
}

