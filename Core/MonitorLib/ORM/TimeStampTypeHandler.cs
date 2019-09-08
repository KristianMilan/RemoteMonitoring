using System;
using Dapper;

namespace Monitoring
{
	public class DateTimeStamp : IComparable<DateTimeStamp>
	{
		public DateTimeStamp()
		{
			Value = DateTime.MinValue;
		}
		public DateTimeStamp(DateTime value)
		{
			Value = value;
		}
		public static bool TryParse(string value, out DateTimeStamp obj)
		{
			try
			{
				obj = Parse(value);
				return true;
			}
			catch {
				try
				{
					DateTime tmpVal = DateTime.Parse(value);
					obj = new DateTimeStamp(tmpVal);
					return true;
				}
				catch {
					obj = null;
					return false;
				}
			}
		}
		public static DateTimeStamp Parse(string value)
		{
			string[] datetimestr = value.Split ("T".ToCharArray());

			int year = DateTime.MinValue.Year;
			int month = DateTime.MinValue.Month;
			int day = DateTime.MinValue.Day;

			int hour = DateTime.MinValue.Hour;
			int minute = DateTime.MinValue.Minute;
			int second = DateTime.MinValue.Second;
			int ms = 0;

			if (datetimestr.Length > 0) {
				string[] dateStr = datetimestr [0].Split ("/".ToCharArray());
				year = int.Parse (dateStr [0]);
				month = int.Parse (dateStr [1]);
				day = int.Parse (dateStr [2]);

				if (datetimestr.Length > 1) {
					string[] sMSStr = datetimestr [1].Split (".".ToCharArray());
					string[] hmsStr = sMSStr [0].Split (":".ToCharArray());

					hour = int.Parse (hmsStr [0]);
					minute = int.Parse (hmsStr [1]);
					second = int.Parse (hmsStr [2]);
					ms = 0;
					if (sMSStr.Length > 1) {
						ms = int.Parse (sMSStr [1]);
					}
				}

				return new DateTimeStamp (new DateTime (year, month, day, hour, minute, second, ms));
			} else
				return new DateTimeStamp ();
		}
		public string ToStandardString()
		{
			if (Value != null)
				return string.Format ("{0}/{1}/{2} {3}:{4}:{5}.{6}",
					Value.Month.ToString ("D2"),
					Value.Day.ToString ("D2"),
					Value.Year.ToString ("D2"), 
					Value.Hour.ToString ("D2"),
					Value.Minute.ToString ("D2"),
					Value.Second.ToString ("D2"),
					Value.Millisecond.ToString ("D3"));
			else
				return string.Empty;
		}
		public override string ToString ()
		{
			if (Value != null)
				return string.Format ("{0}/{1}/{2}T{3}:{4}:{5}.{6}",
					Value.Year.ToString ("D2"), 
					Value.Month.ToString ("D2"),
					Value.Day.ToString ("D2"),
					Value.Hour.ToString ("D2"),
					Value.Minute.ToString ("D2"),
					Value.Second.ToString ("D2"),
					Value.Millisecond.ToString ("D3"));
			else
				return string.Empty;
		}
		public DateTime Value {get;set;}

		public static DateTimeStamp Now
		{
			get
			{
				return new DateTimeStamp(DateTime.Now);
			}
		}
		public static DateTimeStamp MinValue
		{
			get
			{
				return new DateTimeStamp(DateTime.MinValue);
			}
		}
		public static DateTimeStamp MaxValue
		{
			get
			{
				return new DateTimeStamp(DateTime.MaxValue);
			}
		}

		public int CompareTo (DateTimeStamp other)
		{
			return Value.CompareTo (other.Value);
		}
	}
	public class TimeStampTypeHandler : SqlMapper.TypeHandler<DateTimeStamp>
	{
		public TimeStampTypeHandler ()
		{
		}

		#region implemented abstract members of TypeHandler
		public override void SetValue (System.Data.IDbDataParameter parameter, DateTimeStamp value)
		{
			if (parameter.DbType == System.Data.DbType.DateTime || parameter.DbType == System.Data.DbType.DateTime2) {

			}
			parameter.Value = value.ToString ();
		}
		public override DateTimeStamp Parse (object value)
		{
			if (value.GetType ().Equals (typeof(DateTime))) {
				return new DateTimeStamp ((DateTime)value);
			}
			return DateTimeStamp.Parse ((string)value);
		}
		#endregion
	}
}

