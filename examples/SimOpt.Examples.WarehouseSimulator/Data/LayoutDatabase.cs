using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using Vr.WarehouseSimulator.Data.LayoutTables;
using MatthiasToolbox.Logging;
using System.Data;
using System.Data.Linq.Mapping;
using System.Data.OracleClient;

#pragma warning disable 0618 // OracleClient is obsolet - switch to devart dotConnect before migrating to .NET 5.0

namespace Vr.WarehouseSimulator.Data
{
    // [System.Data.Linq.Mapping.DatabaseAttribute(EntityName = "LAYOUT")] // ? devart dotConnect
    // [ProviderAttribute(typeof(Devart.Data.Oracle.Linq.Provider.OracleDataProvider))] // devart dotConnect mapping attribute
    public class LayoutDatabase : DataContext // : Devart.Data.Linq.DataContext // replacement when switching from OracleClient to dotConnect
    {
        #region cvar

        // the mapping source for devart dotConnect
        // private static MappingSource mappingSource = new Devart.Data.Linq.Mapping.AttributeMappingSource();

        // original connection string
        private string connectionString;

        // table mapping for linq to sql
        // public Table<Layout> LayoutTable;
        
        // workaround for OracleClient
        public readonly LayoutWrapper LayoutTable = new LayoutWrapper();
        public readonly GeometryWrapper GeometryTable = new GeometryWrapper();

        #endregion
        #region prop

        public new OracleConnection Connection { get; private set; }

        // table mapping for devart dotConnect
        //public Devart.Data.Linq.Table<Layout> LayoutTable
        //{
        //    get
        //    {
        //        return this.GetTable<Layout>();
        //    }
        //}

        #endregion
        #region ctor

        // ctor for devart dotConnect mapping
        ///// <summary>
        ///// Connect DataContext using given connection string and
        ///// create an instance of LayoutDatabase
        ///// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        ///// </summary>
        ///// <param name="connectionString">a connection string or db filename</param>
        //public LayoutDatabase(string connectionString) : base(connectionString, mappingSource) 
        //{
        //    this.connectionString = connectionString;
        //    this.Log<INFO>("Connected to " + base.Connection.DataSource);
        //}

        /// <summary>
        /// Connect DataContext using given connection string and
        /// create an instance of LayoutDatabase
        /// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        /// </summary>
        /// <param name="connection">a connection string or db filename</param>
        public LayoutDatabase(OracleConnection connection)
            : base(connection)
        {
            this.connectionString = connection.ConnectionString;
            this.Connection = connection;
            this.Log<INFO>("Connected to " + base.Connection.DataSource);
        }

        #endregion
        #region init

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <returns>success flag</returns>
        public bool Initialize()
        {
            return true;
        }

        #endregion
    }
}
