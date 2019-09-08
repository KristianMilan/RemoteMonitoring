using System;
using System.Collections.Generic;
using System.Linq;

namespace Monitoring
{
	public interface ISensorDiscovery
	{
		IEnumerable<string> ListDevices ();

		ILoggableConnection CreateConnection (string key);
	}
	public interface ILoggableConnection
	{
		DeviceMap Map { get; }

		string Key{ get; } 

		DeviceType DeviceType { get; }

		IEnumerable<DeviceLog> GetLog (DateTime startTime, DateTime endTime);

		IConvertible GetValue ();

		IConvertible GetAndLogValue (DateTime logTimeStamp);

		Type DataType { get; }

		IConvertible Max { get; }

		IConvertible Min {get;}

		string Name {get;}

		string Suffix {get;}

		bool IsChartable {get;}
		bool IsInDashboard {get;}
		bool IsJpegFrame {get;}
	}
	public abstract class LoggableConnection<T> : ILoggableConnection where T : IConvertible
	{
		private DeviceMap _cachedMap = null;
		public LoggableConnection ()
		{
		}

		public DeviceMap Map 
		{
			get 
			{
				try
				{
					if (_cachedMap == null) {
						_cachedMap = ORM.Select<DeviceMap> ("DeviceMap", string.Format ("DeviceKey = '{0}'", Key)).FirstOrDefault ();
						if (_cachedMap == null) {
							_cachedMap = new DeviceMap ();
							_cachedMap.Name = string.Format ("{0} for {1}", DeviceType.ToString (), Key);
							_cachedMap.DeviceKey = Key;
							_cachedMap.DeviceTypeEnum = DeviceType;
							_cachedMap.Created = DateTimeStamp.Now;

							_cachedMap.IsNew = true;
							ORM.Update<DeviceMap> ("DeviceMap", _cachedMap);

							_cachedMap = ORM.Select<DeviceMap> ("DeviceMap", string.Format ("DeviceKey = '{0}'", Key)).First ();
						}
					}

					return _cachedMap;
				}
				catch(Exception ex) {
					LogWriter.WriteLog (string.Format ("An error was encountered while attempting to load the map for loggable connection {0}", Key), ex);
					throw ex;
				}
			}
		}

		public abstract string Key{ get; } 

		public abstract DeviceType DeviceType { get; }

		public virtual IEnumerable<DeviceLog> GetLog(DateTime startTime, DateTime endTime)
		{
			return ORM.Select<DeviceLog>("DeviceLog", string.Format ("DeviceMapID = '{0}' AND TimeStamp >= '{1}' AND TimeStamp <= '{2}'", Map.ID, startTime, endTime));
		}

		public IConvertible GetValue()
		{
			return ReadValue ();
		}
		public IConvertible GetAndLogValue(DateTime logTimeStamp)
		{
			return ReadAndLogValue (logTimeStamp);
		}

		public abstract T ReadValue ();

		public virtual T ReadAndLogValue (DateTime logTimeStamp)
		{
			T retVal = ReadValue ();
			Map.LogValue(retVal, logTimeStamp);

			return retVal;
		}

		public abstract IConvertible Max { get; }
		public abstract IConvertible Min {get;}
		public abstract string Name {get;}
		public abstract string Suffix {get;}

		public Type DataType { get { return typeof(T); } }

		public abstract bool IsChartable {get;}
		public abstract bool IsInDashboard {get;}
		public virtual bool IsJpegFrame {get{ return false; }}
	}
}

