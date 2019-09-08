using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;

namespace Monitoring
{
	public abstract class JSONObject
	{
		public virtual string ToJSON ()
		{
			JsonSerializer serializer = new JsonSerializer ();
			StringBuilder sb = new StringBuilder();
			JsonTextWriter w = new JsonTextWriter (new StringWriter (sb));
			serializer.Serialize (w, this);
			w.Flush ();

			return sb.ToString ();
		}

		public static T FromJSON<T> (string json) where T : JSONObject
		{
			return (T)FromJSON (json);
		}
		public static JSONObject FromJSON (string json)
		{
			JsonSerializer serializer = new JsonSerializer ();
			JsonTextReader r = new JsonTextReader (new StringReader (json));
			return (JSONObject)serializer.Deserialize (r);
		}
		public virtual string ContentType
		{
			get {
				return "text/javascript";
			}
		}
		public virtual string Extension
		{
			get
			{
				return "json";
			}
		}
	}

	public class ServiceListJSON : JSONObject, IEnumerable<string>
	{
		private List<string> _serviceList;
		public ServiceListJSON()
		{
			_serviceList = new List<string> ();
		}
		public ServiceListJSON(IEnumerable<Type> ServiceTypes)
		{
			_serviceList = ServiceTypes.Select (s => string.Format ("{0}.json", s.Name)).ToList ();

		}
		#region IEnumerable implementation
		public IEnumerator<string> GetEnumerator ()
		{
			return _serviceList.GetEnumerator ();
		}
		#endregion
		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return _serviceList.GetEnumerator ();
		}
		#endregion
	}

	public interface IJSONRequestImplementationCore
	{
		JSONObject GetResponseJSON(Dictionary<string, string[]> requestAttributes);
	}

	public abstract class JSONRequestImplementation<T> : IJSONRequestImplementationCore where T : JSONObject
	{
		public abstract T GetResponse (Dictionary<string, string[]> requestAttributes);
		public JSONObject GetResponseJSON(Dictionary<string, string[]> requestAttributes)
		{
			if (requestAttributes.ContainsKey ("querykeydetails")) 
				return new QueryKeyInfoList (QueryKeyDetails);
			else
				if (requestAttributes.ContainsKey("serviceinfo"))
					return new QueryKeyInfoList (QueryKeyDetails);
			return GetResponse (requestAttributes);
		}

		public abstract List<QueryKeyInfo> QueryKeyDetails{ get; }
	}

	public class QueryKeyInfoList : JSONObject, IEnumerable<QueryKeyInfo>
	{
		private IEnumerable<QueryKeyInfo> _infoList;
		public QueryKeyInfoList()
		{
			_infoList = new List<QueryKeyInfo> ();
		}
		public QueryKeyInfoList(IEnumerable<QueryKeyInfo> infoList)
		{
			_infoList = infoList;
		}

		#region IEnumerable implementation

		public IEnumerator<QueryKeyInfo> GetEnumerator ()
		{
			return _infoList.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return _infoList.GetEnumerator ();
		}

		#endregion
	}
	public class QueryKeyInfo : JSONObject
	{
		public QueryKeyInfo(string name, string details, Type keyType, bool required)
		{
			KeyName = name;
			Detail = details;
			KeyType = keyType;
			Required = required;
		}
		public string KeyName {get;private set;}
		public string Detail {get;private set;}
		public Type KeyType {get;private set;}
		public bool Required {get;private set;}
	}

	public static class JSONRequestManager
	{
		static JSONRequestManager()
		{
			//Instantiate the lists and dictionary used to manage request handler types.
			JSONObjects = new List<Type> ();
			JSONRequestImplementations = new List<Type> ();
			RequestImplementationMap = new Dictionary<string, Type> ();

			//By default, locate any handlers in this assembly.
			ScanAssemblyForHandlers (typeof(JSONRequestManager).Assembly);
		}

		public static void MapRequestToType(string requestKey, Type requestImplementation)
		{
			requestKey = requestKey.Trim ().ToUpper ();
			if (RequestImplementationMap.ContainsKey(requestKey))
			{
				RequestImplementationMap[requestKey] = requestImplementation;
			}
			else
			{
				RequestImplementationMap.Add(requestKey, requestImplementation);
			}
		}

		public static JSONObject HandleRequest(string requestKey, Dictionary<string, string[]> requestParams)
		{
			requestKey = requestKey.Trim ().ToUpper ();
			if (requestKey.StartsWith("/"))
				requestKey = requestKey.Substring(1, requestKey.Length - 1);

			if (requestKey.StartsWith("\\"))
				requestKey = requestKey.Substring(1, requestKey.Length - 1);

			if (RequestImplementationMap.ContainsKey (requestKey)) {
				var req = CreateImplementation (RequestImplementationMap [requestKey]);
				return req.GetResponseJSON(requestParams);
			} else {
				return null;
			}
		}		

		public static void ScanAssemblyForHandlers(Assembly assembly)
		{
            try
            {
                //Locate JSONObjects and JSONRequestImplementations in the assembly.
                var objects = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                   .Any(i => i.Equals(typeof(JSONObject))))
                    .Where(t => !JSONObjects.Contains(t)).AsEnumerable();

                var handlers = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                       .Any(i => i.Equals(typeof(IJSONRequestImplementationCore))) && t.IsAbstract == false && t.IsInterface == false)
                    .Where(t => !JSONObjects.Contains(t)).AsEnumerable();

                //Add the JSONObjects and JSONRequestImplementations to their respective caches.
                JSONObjects.AddRange(objects);
                JSONRequestImplementations.AddRange(handlers);

                //Map each handler to it's json formatted name ([TypeName].json)
                foreach (var handler in handlers)
                {
                    string hName = handler.Name;
                    if (hName.Contains("`"))
                        hName = hName.Substring(0, hName.IndexOf("`"));
                    MapRequestToType(string.Format("{0}.json", hName), handler);
                }
            }

            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                throw ex;
            }
		}

		public static JSONObject ListServices ()
		{
			return new ServiceListJSON (JSONRequestImplementations);
		}

		public static List<Type> JSONObjects{get;set;}
		public static List<Type> JSONRequestImplementations {get;set;}
		public static Dictionary<string, Type> RequestImplementationMap {get;set;}

		private static IJSONRequestImplementationCore CreateImplementation(Type reqImplementationType)
		{
			return (IJSONRequestImplementationCore)reqImplementationType.Assembly.CreateInstance(reqImplementationType.FullName,true);
		}
	}

}

