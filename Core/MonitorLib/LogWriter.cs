using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Monitoring
{
	public static class LogWriter
	{
		private static StreamWriter _logWriter = null;
		private static FileInfo _logFileInfo = null;

		static LogWriter ()
		{
		}


		public static void WriteLog(string logText)
		{
			WriteLog (logText, false, false);
		}
		public static void WriteLog(string logText, Exception ex)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (logText);
			sb.Append (ParseException (ex));

			WriteLog (sb.ToString (), false, true);
		}

		public static void WriteLog(string logText, bool debug, bool error)
		{
			var previousLED = StatusLED.GetColor ();

			if (error) {
				StatusLED.SetColor (true, false, false);
			} else if (debug) {
				StatusLED.SetColor (false, true, true);
			}

			var currentLED = StatusLED.GetColor ();

			string logLine = "";
			if (_logFileInfo == null || _logFileInfo.Name != LogFileName()) {
				_logFileInfo = new FileInfo (LogFileName());

				if (_logWriter != null) {
					_logWriter.Flush ();
					_logWriter.Close ();
				}

				if (!_logFileInfo.Exists)
					_logWriter = new StreamWriter (_logFileInfo.Create ());
				else
					_logWriter = new StreamWriter (_logFileInfo.OpenWrite ());
			}

			if (!error)
			{
				if (!debug)
					logLine = string.Format ("{0} - INFO - {1}", DateTime.Now, logText);
					
				else
					logLine = string.Format ("{0} - DEBUG - {1}", DateTime.Now, logText);
			}
			else
			{
				logLine = string.Format ("{0} - ERROR - {1}", DateTime.Now, logText);
			}

			_logWriter.WriteLine (logLine);
			_logWriter.Flush ();
			Console.WriteLine (logLine);

			Thread colorThread = new Thread (new ThreadStart (delegate() {
				Thread.Sleep (500);
				if (currentLED == StatusLED.GetColor ()) {
					StatusLED.SetColor (previousLED);
				}
			}));
			colorThread.Start ();

		}

		private static string LogFileName()
		{
			return string.Format ("webhost_{0}_{1}_{2}.log", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
		}

		private static string ParseException(Exception ex)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (" Exception: ");
			sb.Append (ex.Message);
			sb.Append (" - Source:");
			sb.Append (ex.Source);
			sb.Append (" StackTrace:");
			sb.Append (ex.StackTrace);
			if (ex.InnerException != null)
				sb.Append (ParseException (ex.InnerException));

			return sb.ToString ();
		}
	}
}

