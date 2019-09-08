using System;
using Monitoring;
using System.Linq;

namespace Monitoring
{
	public class Configuration : PersistableObject
	{
		public Configuration ()
		{
		}

		#region implemented abstract members of PersistableObject

		public override void GenerateKeys ()
		{
			//IDGuid = Guid.NewGuid ();
		}

		#endregion

		public int ID {get;set;}
		public DateTime Created {get;set;}
		public string Key {get;set;}
		public string Identifier {get;set;}
		public string Value {get;set;}

		public static bool SetConfiguration(string key, string value)
		{
			return SetConfiguration (key, null, value);
		}

		public static bool SetConfiguration(string key, string identifier, string value)
		{
			Configuration retConfig = null;
			if (!string.IsNullOrEmpty (identifier)) 
				retConfig = ORM.Select<Configuration> ("Configuration", string.Format ("Key = '{0}' AND Identifier = '{1}'", key, identifier)).FirstOrDefault();
			 else 
				retConfig = ORM.Select<Configuration> ("Configuration", string.Format ("Key = '{0}' AND Identifier = NULL", key)).FirstOrDefault();

			if (retConfig == null) {
				retConfig = new Configuration (){ Key = key, Identifier = identifier, Created = DateTime.Now };
			}

			retConfig.Value = value;

			return ORM.Update<Configuration> ("Configuration", retConfig);

		}

		public static Configuration GetConfiguration(string key, string identifier)
		{
			Configuration retConfig = null;
			if (!string.IsNullOrEmpty (identifier)) 
				retConfig = ORM.Select<Configuration> ("Configuration", string.Format ("Key = '{0}' AND Identifier = '{1}'", key, identifier)).FirstOrDefault();
			else 
				retConfig = ORM.Select<Configuration> ("Configuration", string.Format ("Key = '{0}' AND Identifier = NULL", key)).FirstOrDefault();

			return retConfig;
		}
	}
}

