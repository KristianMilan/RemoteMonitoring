using System;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using Dapper;
using DapperExtensions;

namespace Monitoring
{
	public class ORM
	{
		private static string _cachedConnString = null;
		private static DbProviderFactory _cachedProvider = null;
		static ORM()
		{
			LoadCheckConnString();

			//System.Data.SQLite.SQLiteLog.Enabled = true;
			//System.Data.SQLite.SQLiteLogEventHandler handler = new System.Data.SQLite.SQLiteLogEventHandler (delegate(object sender, System.Data.SQLite.LogEventArgs e) {
			//	LogWriter.WriteLog (string.Format ("SQLite Log Entry: {0} - {1} {2} {3}", e.Message, e.ErrorCode, e.Data, e.GetHashCode ()));
			//});

			//TODO: Change this to dynamically set the dialect
			DapperExtensions.DapperExtensions.SqlDialect = new DapperExtensions.Sql.SqliteDialect();

			SqlMapper.AddTypeHandler(new TimeStampTypeHandler());
		}

		private static void LoadCheckConnString()
		{
			if (_cachedConnString == null && System.Configuration.ConfigurationManager.ConnectionStrings.Count > 0)
			{
				_cachedConnString = System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString;
				_cachedProvider = DbProviderFactories.GetFactory (System.Configuration.ConfigurationManager.ConnectionStrings [0].ProviderName);
			}
			if (_cachedConnString == null) {
				throw new ConnectionStringLoadException ();
			}
		}

		public ORM ()
		{

		}
		public ORM (string connectionString, string providerName)
		{
			_cachedProvider = DbProviderFactories.GetFactory (providerName);
			_cachedConnString = connectionString;
		}

		public static DbConnection CreateConnection()
		{
			LoadCheckConnString ();
			DbConnection conn = _cachedProvider.CreateConnection ();
			conn.ConnectionString = _cachedConnString;

			return conn;
		}

		public static DbCommand CreateCommand(string commandText, ref DbConnection connection)
		{
			DbCommand cmd = _cachedProvider.CreateCommand ();
			cmd.Connection = connection;
			cmd.CommandText = commandText;

			return cmd;
		}

		public static IEnumerable<T> Select<T>(string tableName, string filter) where T: PersistableObject
		{
			DbConnection conn = CreateConnection ();
			IEnumerable<T> retVal;

			conn.Open ();
			retVal = Select<T> (tableName, filter, ref conn);
			conn.Close ();

			return retVal;
		}
		public static IEnumerable<T> Select<T>(string tableName, string filter, ref DbConnection conn) where T: PersistableObject
		{
			try
			{
				if (filter.Trim().ToUpper().StartsWith("WHERE"))
					filter = filter.Trim().Substring(5, filter.Trim().Length - 5);

				//PetaPOCO syntax
				//var db = new PetaPoco.Database(conn.ConnectionString, _cachedProvider);
				//var retValues = db.Query<T>(string.Format("Select * from {0} where {1}", tableName, filter));

				//Dapper syntax
				IEnumerable<T> retValues;
				if (string.IsNullOrEmpty(filter))
					retValues = conn.Query<T>(string.Format ("Select * from {0}", tableName),null,null, true, null, CommandType.Text);
				else
					retValues = conn.Query<T>(string.Format ("Select * from {0} where {1}", tableName, filter),null,null, true, null, CommandType.Text);

				foreach(var val in retValues)
				{
					val.IsNew = false;
				}

				return retValues;
			}
			catch(Exception ex) {
				LogWriter.WriteLog (string.Format ("Error while attempting to select from {0} - filter: {1}.", tableName, filter), ex);
				throw;
			}
		}

		public static bool Update<T>(string tableName, T item) where T:PersistableObject
		{
			List<T> list = new List<T>();
			list.Add(item);
			return Update<T>(tableName, list);
		}

		public static bool Update<T>(string tableName, IEnumerable<T> objects) where T:PersistableObject
		{
			bool ret = true;
			DbConnection conn = CreateConnection ();
			conn.Open ();
			using (DbTransaction trans = conn.BeginTransaction ()) {
				try
				{
					ret = ret && Update<T>(tableName, objects, trans, ref conn);
				}
				catch(Exception ex) {
					LogWriter.WriteLog (string.Format ("Error while attempting to update to {0}.", tableName), ex);
					trans.Rollback ();
					conn.Close ();
					throw ex;
				}

				trans.Commit ();
				conn.Close ();
			}
			return ret;
		}

		public static bool Update<T>(string tableName, T item, DbTransaction transaction, ref DbConnection conn) where T:PersistableObject
		{
			List<T> list = new List<T>();
			list.Add(item);
			return Update<T>(tableName, list, transaction, ref conn);
		}

		public static bool Update<T>(string tableName, IEnumerable<T> objects, DbTransaction transaction, ref DbConnection conn) where T:PersistableObject
		{
			bool ret = true;
			foreach (var obj in objects) {

				if (obj.IsNew && !obj.MarkForDelete) {
					try {
						//Insert the object

						//PetaPOCO syntax
						//PetaPoco.Database db = new Database(conn.ConnectionString, _cachedProvider);
						//var newPK = db.Insert(obj);

						// //Dapper syntax
						conn.Insert<T>(obj, transaction, 100);
					} catch (Exception ex) {
						ret = false;
						LogWriter.WriteLog (string.Format ("Error while attempting to insert into {0}.", tableName), ex);
						throw ex;
					}
				} else {
					if (obj.MarkForDelete) {
						try {
							//Delete the object

							//PetaPOCO syntax
							//PetaPoco.Database db = new Database(conn);
							//ret = ret && db.Delete(obj) > 0;

							//Dapper syntax
							ret = ret && conn.Delete<T>(obj, transaction, null);

						} catch (Exception ex) {
							LogWriter.WriteLog (string.Format ("Error while attempting to delete from {0}.", tableName), ex);
							ret = false;
							throw ex;
						}
					} else {
						try {
							//Update the object

							//PetaPOCO syntax
							//PetaPoco.Database db = new Database(conn);
							//ret = ret && db.Update(obj) > 0;

							//Dapper syntax
							ret = ret || conn.Update(obj, transaction, null);
						} catch (Exception ex) {
							LogWriter.WriteLog (string.Format ("Error while attempting to update {0}.", tableName), ex);
							ret = false;
							throw ex;
						}
					}
				}
			}
			return ret;
		}
	}

	public abstract class PersistableObject
	{
		public PersistableObject() : this(true)
		{
		}
		public PersistableObject(bool isNew)
		{
			IsNew = isNew;
			MarkForDelete = false;
			if (isNew)
				GenerateKeys ();
		}

		protected internal bool IsNew{get;set;}
		protected internal bool MarkForDelete{get;set;}

		public abstract void GenerateKeys ();
	}

	public class ConnectionStringLoadException : Exception
	{
		public ConnectionStringLoadException() : base("The connection string was not found in the configuration data, or was not created on initialization.")
		{
		}
	}
}

