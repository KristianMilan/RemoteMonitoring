using System;
using System.Drawing;
using System.IO;

namespace Monitoring
{
	public class DeviceLog : PersistableObject
	{
		public DeviceLog ()
		{
		}

		#region implemented abstract members of PersistableObject

		public override void GenerateKeys ()
		{
			//IDGuid = Guid.NewGuid ();
		}

		#endregion

		public int ID {get;set;}
		public DateTimeStamp TimeStamp {get;set;}
		public int DeviceMapID {get;set;}
		public string Value {get;set;}

		public double ToDouble()
		{
			double val = 0.0f;

			double.TryParse (Value, out val);
			return val;
		}

		public Image ToImage()
		{
			try
			{
				MemoryStream str = new MemoryStream();
				byte[] buff = Convert.FromBase64String(Value);
				str.Write(buff,0,buff.Length);
				str.Flush();
				str.Position = 0;

				Image retImg = Image.FromStream(str);
				str.Flush();
				return retImg;
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("An error was encountered while attempting to load the device image.", ex);
				return null;
			}
		}
	}
}

