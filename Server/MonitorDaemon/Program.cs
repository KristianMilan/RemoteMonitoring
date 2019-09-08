using System;
using System.Net;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Configuration;
using System.Reflection;
using Monitoring;

namespace AquaponicsDaemon
{
	class MainClass
	{
		private static HttpListener _listener;
		private static bool _isRunning = true;
		private static DirectoryInfo webDir = new DirectoryInfo ("Web");

		public static void Main (string[] args)
		{
			try
			{
				int deviceCount = MonitoringManager.DeviceKeys.Count();
				if (deviceCount == 0)
				{
					LogWriter.WriteLog("Monitoring manager reports nothing to monitor.  At minimum drive free space should be monitored.");
				}
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("Error was encountered while initializing the monitoring manager.", ex);
			}

			try
			{
				if (!webDir.Exists)
					webDir.Create();
			}
			catch(Exception ex) {
				LogWriter.WriteLog ("Error was encountered while attempting to get the default hosting directory 'Web'", ex);
			}

			LogWriter.WriteLog ("Starting Aquaponics Daemon");


			try
			{
				_listener = new HttpListener();
				if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ListenURL"]))
				{
					_listener.Prefixes.Add (ConfigurationManager.AppSettings["ListenURL"].ToString());
				}
				else
				{
					_listener.Prefixes.Add ("http://localhost:8000/");
					_listener.Prefixes.Add ("http://0.0.0.0:8000/");
					_listener.Prefixes.Add ("http://*:8000/");
				}
				_listener.Start ();
				LogWriter.WriteLog ("Listening on all addresses.");
			}
			catch(Exception ex) {
				LogWriter.WriteLog("Could not start listening on all addresses.  Reverting to local only.",ex);
				_listener = new HttpListener();
				_listener.Prefixes.Add ("http://localhost:8000/");
				_listener.Start ();
				LogWriter.WriteLog ("Listening on localhost.");
			}
//			try
//			{
//			_listener.Prefixes.Add ("http://localhost:80/");
//			}
//			catch(Exception ex) {
//				LogWriter.WriteLog ("Error attempting to listen on port 80", ex);
//			}


			while (_isRunning) {
				var context = _listener.GetContext ();
				HandleContext (context);
			}
		}


		private static void HandleContext(HttpListenerContext context)
		{
			//WaitCallback cb = new WaitCallback (state => HandleContextAsync (context, state));

			//ThreadPool.QueueUserWorkItem (cb);

			HandleContextAsync (context, null);

		}

		static void HandleJSONRequest (HttpListenerContext context)
		{
			StreamWriter outWriter;
			Dictionary<string, string[]> parameters = new Dictionary<string, string[]> ();
			for(int x = 0; x < context.Request.QueryString.Count; x++)
			{
				var key = context.Request.QueryString.GetKey(x);
				var values = context.Request.QueryString.GetValues (x);

				if (key == null && !parameters.ContainsKey(values.FirstOrDefault()))
				{
					parameters.Add (values.FirstOrDefault().Trim().ToLower(), values);
				} else {
					parameters.Add (key.Trim().ToLower(), values);
				}

			}

            JSONObject result = null;
            try
            {
                result = JSONRequestManager.HandleRequest(context.Request.Url.AbsolutePath, parameters);
            }
            catch(Exception ex)
            {
                throw ex;
            }

			if (result != null) {
				string json = result.ToJSON ();

				context.Response.ContentLength64 = json.Length;
				context.Response.Headers.Add (HttpResponseHeader.ContentType, result.ContentType);

				context.Response.AddHeader 
					("content-disposition", 
					string.Format ("attachment;filename={0}.{1}", 
						context.Request.Url.AbsolutePath.Replace (".json", ""), 
						result.Extension));

				outWriter = new StreamWriter (context.Response.OutputStream);
				outWriter.Write (json);
				outWriter.Flush ();
			} else {
				context.Response.StatusCode = 404;
			}

		}

		private static void HandleContextAsync(HttpListenerContext context, object state)
		{
			string reqFile = context.Request.Url.AbsolutePath;
			if (reqFile.StartsWith ("/"))
				reqFile = reqFile.Remove (0, 1);
			string filePath = Path.Combine (webDir.FullName, reqFile);
			FileInfo fileInf = new FileInfo (filePath);
			bool handled = false;
			bool isJSON = false;

			if (reqFile.Trim ().ToUpper () == "SERVICEINFO" && !handled) {
				ListServices (ref context);
			}

			isJSON = isJSON || (context.Request.ContentType != null && context.Request.ContentType.Replace (@"/", "|").Replace (@"\", "|").Trim ().ToUpper () == "APPLICATION|JSON");
			isJSON = isJSON || context.Request.RawUrl.Trim ().ToUpper ().Contains (".JSON");

			if (isJSON && !handled) {
				handled = true;
				HandleJSONRequest (context);
			}

			if (fileInf.Exists && !handled) {
				handled = true;
				WriteFile (fileInf.FullName, ref context);
			}


			if (reqFile.Length <= 1 && webDir.GetFiles ("index.html").Any () && !handled) {
				handled = true;
				WriteFile (webDir.GetFiles ("index.html") [0].FullName, ref context);
			}

			if (reqFile.Length <= 1 && webDir.GetFiles ("index.htm").Any () && !handled) {
				handled = true;
				WriteFile (webDir.GetFiles ("index.htm") [0].FullName, ref context);
			}
				

			if (!handled) {
				context.Response.StatusCode = 404;
			}


			context.Response.Close ();
		}

		private static void ListServices(ref HttpListenerContext context)
		{
			StreamWriter outWriter = new StreamWriter (context.Response.OutputStream);
			string serviceListJSON = JSONRequestManager.ListServices ().ToJSON ().Trim();

			context.Response.Headers.Add (HttpResponseHeader.ContentType, "text/javascript");
			context.Response.ContentLength64 =  serviceListJSON.Length;

			outWriter.Write (serviceListJSON);

			outWriter.Flush ();
			outWriter.Close ();
		}
		private static void WriteFile(string file, ref HttpListenerContext context)
		{
			FileInfo fileInf = new FileInfo(file);
			BinaryWriter outWriter = new BinaryWriter (context.Response.OutputStream);
			BinaryReader rdr = new BinaryReader (fileInf.OpenRead ());

			context.Response.ContentLength64 =  rdr.BaseStream.Length;
			context.Response.Headers.Add (HttpResponseHeader.ContentType, MIMELookup (fileInf.Extension));

			outWriter.Write (rdr.ReadBytes ((int)rdr.BaseStream.Length));

			rdr.Close ();
			rdr = null;

			outWriter.Flush ();
			outWriter.Close ();
		}


		private static string MIMELookup(string extension)
		{
			extension = extension.Replace (".", "").Trim ().ToUpper ();

			switch (extension) {
			case "JS":
				return "application/javascript";
			case "JSON":
				return "application/json";
			case "XML":
				return "application/xml";
			case "XSLT":
				return "application/xslt+xml";
			case "CSS":
				return "text/css";
			case "XHTML":
				return "application/xhtml+xml";
			case "HTM":
				return "text/html";
			case "HTML":
				return "text/html";
			case "PDF":
				return "application/pdf";
			case "SWF":
				return "application/x-shockwave-flash";
			case "XAP":
				return "application/x-silverlight-app";
			case "ZIP":
				return "application/zip";
			case "GZ":
				return "application/gzip";
			case "BZ":
				return "application/bzip2";
			case "PNG":
				return "image/png";
			case "JPG":
				return "image/jpeg";
			case "JPEG":
				return "image/jpeg";
			case "JFIF":
				return "image/jpeg";
			case "GIF":
				return "image/gif";
			case "G3":
				return "image/g3fax";
			case "ICO":
				return "image/ico";
			case "TIFF":
				return "image/tiff";
			case "TIF":
				return "image/tiff";
			case "XBM":
				return "image/x-xbitmap";
			case "XPM":
				return "image/x-xpixmap";
			case "XIF":
				return "image/vnd.xiff";
			case "BMP":
				return "image/bmp";
			case "BM":
				return "image/bmp";
			case "RGB":
				return "image/x-rgb";
			case "PCT":
				return "image/x-pct";
			case "PCX":
				return "image/x-pcx";
			case "PIC":
				return "image/pict";
			case "PICT":
				return "image/pict";
			case "DXF":
				return "image/vnd.dwg";
			case "SVF":
				return "image/vnd.dwg";
			case "SVG":
				return "image/svg+xml";
			default:
				return "application/octet-stream";
			}
		}

	}
}
