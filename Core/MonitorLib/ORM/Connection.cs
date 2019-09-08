using System;

namespace AquaponicsMonitor
{
	public class Connection : PersistableObject
	{
		public Connection ()
		{
		}

		#region implemented abstract members of PersistableObject

		public override void GenerateKeys ()
		{
			ID = Guid.NewGuid ();
		}

		#endregion

		public Guid ID {get;private set;}
		public string Name {get;set;}
		public string ClassType {get;set;}
		public byte[] Serialized {get;set;}

	}
}

