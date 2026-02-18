using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using MatthiasToolbox.Logging;
using MatthiasToolbox.SupplyChain.Database.DataExtensions;
using MatthiasToolbox.SupplyChain.Interfaces;
using MatthiasToolbox.SupplyChain.Database.ModelTables;

namespace MatthiasToolbox.SupplyChain.Database
{
    public class ModelDatabase : DataContext, ILogger
    {
        #region cvar

        #region connection

        private string connectionString;
        private bool isConnected;

        #endregion
        #region tables

        // settings and audit data
        public readonly Table<DatabaseLog> DatabaseLogTable;
        public readonly Table<Setting> SettingTable;

        // data extensions
        public readonly Table<DataExtension> DataExtensionTable;
        public readonly Table<StringDataExtension> StringDataExtensionTable;
        public readonly Table<BooleanDataExtension> BooleanDataExtensionTable;
        public readonly Table<IntegerDataExtension> IntegerDataExtensionTable;
        public readonly Table<LongDataExtension> LongDataExtensionTable;
        public readonly Table<FloatDataExtension> FloatDataExtensionTable;
        public readonly Table<DoubleDataExtension> DoubleDataExtensionTable;
        public readonly Table<DecimalDataExtension> DecimalDataExtensionTable;

        // model tables
        public readonly Table<ShelfLifeZone> ShelfLifeZoneTable;
        public readonly Table<ShelfLife> ShelfLifeTable;
        public readonly Table<PackageCategory> PackageCategoryTable;
        public readonly Table<ProductCategory> ProductCategoryTable;
        public readonly Table<Product> ProductTable;
        public readonly Table<PackageSize> PackageSizeTable;
        public readonly Table<DepotCategory> DepotCategoryTable;
        public readonly Table<Depot> DepotTable;
        public readonly Table<SiteCategory> SiteCategoryTable;
        public readonly Table<Site> SiteTable;
        public readonly Table<CustomerCategory> CustomerCategoryTable;
        public readonly Table<CustomerDistribution> CustomerDistributionTable;
        public readonly Table<RandomizationBlock> RandomizationBlockTable;
        public readonly Table<Area> AreaTable;
        public readonly Table<TrialSchedule> TrialScheduleTable;
        public readonly Table<ProductionSchedule> ProductionScheduleTable;

        #endregion

        #endregion
        #region prop

        #region Main

        public string CurrentUser { get; private set; }
        public string ConnectionString { get { return connectionString; } }
        public bool IsConnected { get { return isConnected; } }
        public bool Initialized { get; private set; }

        #endregion
        #region Logging

        public bool LoggingEnabled { get; set; }
        public static long SessionID { get; private set; }

        #endregion
        #region DataExtensions

        public Dictionary<string, Dictionary<string, DataExtension>> DataExtensionsByTableName { get; private set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Connect DataContext using given connection string and
        /// create an instance of IndexDatabase
        /// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        /// </summary>
        /// <param name="connectionString">a connection string or db filename</param>
        public ModelDatabase(string connectionString, string currentUser)
            : base(connectionString)
        {
            DataExtensionsByTableName = new Dictionary<string, Dictionary<string, DataExtension>>();

            CurrentUser = currentUser;

            this.connectionString = connectionString;
            this.isConnected = true;
            this.Log<INFO>("Connected to " + base.Connection.DataSource);
        }

        #endregion
        #region init

        /// <summary>
        /// deletes the db
        /// </summary>
        public void DeleteDB()
        {
            if (DatabaseExists()) DeleteDatabase();
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <returns>success flag</returns>
        public bool Initialize()
        {
            if (Initialized)
            {
#if DEBUG
                this.Log<WARN>("The ModelDatabase was already initialized.");
#endif
                return true;
            }

            try
            {
                if (!DatabaseExists()) MakeDB();

                if (DatabaseLogTable.Any()) SessionID = (from log in DatabaseLogTable select log.SessionID).Max() + 1;

                DatabaseLogTable.DeleteAllOnSubmit((from row in DatabaseLogTable where row.SessionID < SessionID - 2 select row));
                SubmitChanges();
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Error opening DB connection or reading data: ", e);
                return false;
            }

            LoadMemoryData();

            Initialized = true;
            return true;
        }

        private bool MakeDB()
        {
            try
            {
                CreateDatabase();
                CreateDefaultData();
                SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                this.Log<FATAL>(ex);
                return false;
            }
        }

        #endregion
        #region impl

        #region default data factories

        public ShelfLifeZone ShelfLifeZoneWorldWide { get; private set; }

        private void CreateDefaultData()
        {
            // shelf life zones
            ShelfLifeZoneWorldWide = new ShelfLifeZone("Worldwide", 0, "predefined");
            ShelfLifeZoneTable.InsertOnSubmit(ShelfLifeZoneWorldWide);

            // randomization
            RandomizationBlock defaultBlock = new RandomizationBlock("Default Block", 1);
            RandomizationBlockTable.InsertOnSubmit(defaultBlock);
            SubmitChanges();

            // areas
            Area defaultArea = new Area("Default Area", defaultBlock);
            AreaTable.InsertOnSubmit(defaultArea);

            SubmitChanges();

            // depots
            DepotCategory centralDepotCategory = new DepotCategory("Central Depot");
            DepotCategory defaultDepotCategory = new DepotCategory("Normal Depot");
            DepotCategoryTable.InsertOnSubmit(centralDepotCategory);
            DepotCategoryTable.InsertOnSubmit(defaultDepotCategory);
            SubmitChanges();

            Depot defaultDepot = new Depot("Default Central Depot", centralDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            Depot depot1 = new Depot("Depot 1", defaultDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            Depot depot2 = new Depot("Depot 2", defaultDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            Depot depot3 = new Depot("Depot 3", defaultDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            Depot depot4 = new Depot("Depot 4", defaultDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            Depot depot5 = new Depot("Depot 5", defaultDepotCategory, DateTime.Now, new TimeSpan(5, 6, 0, 0), new TimeSpan(4, 30, 0), defaultArea, ShelfLifeZoneWorldWide);
            DepotTable.InsertOnSubmit(defaultDepot);
            DepotTable.InsertOnSubmit(depot1);
            DepotTable.InsertOnSubmit(depot2);
            DepotTable.InsertOnSubmit(depot3);
            DepotTable.InsertOnSubmit(depot4);
            DepotTable.InsertOnSubmit(depot5);

            // site categories
            SiteCategory defaultSiteCategory = new SiteCategory("Normal Clinic");
            SiteCategoryTable.InsertOnSubmit(defaultSiteCategory);

            SubmitChanges();

            // sites
            Site defaultSite = new Site("Default Site", defaultDepot, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site1 = new Site("Site 1", defaultDepot, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site2 = new Site("Site 2", depot2, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site3 = new Site("Site 3", depot2, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site4 = new Site("Site 4", depot3, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site5 = new Site("Site 5", depot1, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site6 = new Site("Site 6", depot4, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            Site site7 = new Site("Site 7", depot5, defaultSiteCategory, defaultArea, true, 5, 4, 3, 9, 12, new TimeSpan(3, 2, 15, 0));
            SiteTable.InsertOnSubmit(defaultSite);
            SiteTable.InsertOnSubmit(site1);
            SiteTable.InsertOnSubmit(site2);
            SiteTable.InsertOnSubmit(site3);
            SiteTable.InsertOnSubmit(site4);
            SiteTable.InsertOnSubmit(site5);
            SiteTable.InsertOnSubmit(site6);
            SiteTable.InsertOnSubmit(site7);

            // packages
            PackageCategory defaultPackType = new PackageCategory("Default");
            PackageCategory placeboPackType = new PackageCategory("Placebo");
            PackageCategoryTable.InsertOnSubmit(defaultPackType);
            PackageCategoryTable.InsertOnSubmit(placeboPackType);

            // products
            ProductCategory productType1 = new ProductCategory("Tamiflu(R)", 1, 1, 0, 0);
            ProductCategory productType2 = new ProductCategory("Avastin(R)", 1, 1, 0, 0);
            ProductCategoryTable.InsertOnSubmit(productType1);
            ProductCategoryTable.InsertOnSubmit(productType2);

            Product product1 = new Product("Product A");
            Product product2 = new Product("Product B");
            Product product3 = new Product("Product C");
            ProductTable.InsertOnSubmit(product1);
            ProductTable.InsertOnSubmit(product2);
            ProductTable.InsertOnSubmit(product3);

            CustomerCategory customerCategory1 = new CustomerCategory("Default Patient Category");
            CustomerCategoryTable.InsertOnSubmit(customerCategory1);

            SubmitChanges();

            PackageSize size1 = new PackageSize(product1, 12, "á 50mg");
            PackageSize size2 = new PackageSize(product1, 12, "á 100mg");
            PackageSize size3 = new PackageSize(product1, 20, "á 50mg");
            PackageSize size4 = new PackageSize(product2, 25, "á 50mg");
            PackageSize size5 = new PackageSize(product3, 50, "á 50mg");

            PackageSizeTable.InsertOnSubmit(size1);
            PackageSizeTable.InsertOnSubmit(size2);
            PackageSizeTable.InsertOnSubmit(size3);
            PackageSizeTable.InsertOnSubmit(size4);
            PackageSizeTable.InsertOnSubmit(size5);

            CustomerDistribution customerDistribution1 = new CustomerDistribution(defaultArea, customerCategory1, 0, 0);
            CustomerDistributionTable.InsertOnSubmit(customerDistribution1);

            ShelfLife defaultShelfLife = new ShelfLife(ShelfLifeZoneWorldWide, product1, new TimeSpan(14, 0, 0, 0).Ticks);
            ShelfLifeTable.InsertOnSubmit(defaultShelfLife);

            TrialSchedule ts = new TrialSchedule(defaultSite, DateTime.Now, DateTime.Now.AddDays(120));
            TrialScheduleTable.InsertOnSubmit(ts);

            ProductionSchedule ps = new ProductionSchedule(defaultDepot, DateTime.Now, DateTime.Now.AddDays(120));
            ProductionScheduleTable.InsertOnSubmit(ps);

            SubmitChanges();
        }

        #endregion
        #region DataExtensions

        #region string

        public bool CreateStringDataExtension(string forTableName, string name, string defaultValue = "")
        {
            if (DataExtensionsByTableName.ContainsKey(forTableName) && DataExtensionsByTableName[forTableName].ContainsKey(name))
                throw new InvalidOperationException("A data extension with the name " + name + " already exists for the table " + forTableName);
            
            DataExtension de = new DataExtension(name, forTableName, DataExtensionType.String, false);
            StringDataExtension sde = new StringDataExtension(de, 0, defaultValue);

            try
            {
                DataExtensionTable.InsertOnSubmit(de);
                StringDataExtensionTable.InsertOnSubmit(sde);

                SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to create data extension.", ex);
                return false;
            }

            if (!DataExtensionsByTableName.ContainsKey(forTableName)) DataExtensionsByTableName[forTableName] = new Dictionary<string, DataExtension>();
            DataExtensionsByTableName[forTableName][name] = de;

            return true;
        }

        public bool CreateStringsDataExtension(string forTableName, string name, string defaultValue = "")
        {
            if (DataExtensionsByTableName.ContainsKey(forTableName) && DataExtensionsByTableName[forTableName].ContainsKey(name))
                throw new InvalidOperationException("A data extension with the name " + name + " already exists for the table " + forTableName);

            DataExtension de = new DataExtension(name, forTableName, DataExtensionType.String, true);
            StringDataExtension sde = new StringDataExtension(de, 0, defaultValue);

            try
            {
                DataExtensionTable.InsertOnSubmit(de);
                StringDataExtensionTable.InsertOnSubmit(sde);

                SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to create data extension.", ex);
                return false;
            }

            if (!DataExtensionsByTableName.ContainsKey(forTableName)) DataExtensionsByTableName[forTableName] = new Dictionary<string, DataExtension>();
            DataExtensionsByTableName[forTableName][name] = de;

            return true;
        }

        #endregion
        #region bool

        public bool CreateBooleanDataExtension(string forTableName, string name, bool defaultValue = false)
        {
            if (DataExtensionsByTableName.ContainsKey(forTableName) && DataExtensionsByTableName[forTableName].ContainsKey(name))
                throw new InvalidOperationException("A data extension with the name " + name + " already exists for the table " + forTableName);

            DataExtension de = new DataExtension(name, forTableName, DataExtensionType.Boolean, false);
            BooleanDataExtension sde = new BooleanDataExtension(de, 0, defaultValue);

            try
            {
                DataExtensionTable.InsertOnSubmit(de);
                BooleanDataExtensionTable.InsertOnSubmit(sde);

                SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to create data extension.", ex);
                return false;
            }

            if (!DataExtensionsByTableName.ContainsKey(forTableName)) DataExtensionsByTableName[forTableName] = new Dictionary<string, DataExtension>();
            DataExtensionsByTableName[forTableName][name] = de;

            return true;
        }

        public bool CreateBooleansDataExtension(string forTableName, string name, bool defaultValue = false)
        {
            if (DataExtensionsByTableName.ContainsKey(forTableName) && DataExtensionsByTableName[forTableName].ContainsKey(name))
                throw new InvalidOperationException("A data extension with the name " + name + " already exists for the table " + forTableName);

            DataExtension de = new DataExtension(name, forTableName, DataExtensionType.Boolean, true);
            BooleanDataExtension sde = new BooleanDataExtension(de, 0, defaultValue);

            try
            {
                DataExtensionTable.InsertOnSubmit(de);
                BooleanDataExtensionTable.InsertOnSubmit(sde);

                SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to create data extension.", ex);
                return false;
            }

            if (!DataExtensionsByTableName.ContainsKey(forTableName)) DataExtensionsByTableName[forTableName] = new Dictionary<string, DataExtension>();
            DataExtensionsByTableName[forTableName][name] = de;

            return true;
        }

        #endregion

        #endregion
        #region ILogger

        void ILogger.Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (data.ContainsKey("LogToDB") && data["LogToDB"].GetType() == typeof(bool) && (bool)data["LogToDB"] == false)
                return;

            int severity = 0;
            if (data.ContainsKey("Severity") && data["Severity"].GetType() == typeof(int))
                severity = (int)data["Severity"];

            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            DatabaseLog dbl = new DatabaseLog(timeStamp.Ticks, severity, messageClass.Name, message, s);

            try
            {
                DatabaseLogTable.InsertOnSubmit(dbl);
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Database logging error: ", ex, LogToDB => false);
            }
        }

        public void ShutdownLogger()
        {
            LoggingEnabled = false;
            try
            {
                SubmitChanges();
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
            }
        }

        #endregion

        #endregion
        #region util

        private void LoadMemoryData()
        {
            // data extensions
            IQueryable<string> tables = (from row in DataExtensionTable
                                   select row.TableName).Distinct();
            foreach (string tableName in tables) 
            {
                DataExtensionsByTableName[tableName] =
                    (from row in DataExtensionTable
                     where row.TableName == tableName
                     select row).ToDictionary(de => de.Name);
            }

            // common fields
            ShelfLifeZoneWorldWide = (from row in ShelfLifeZoneTable where row.ID == 1 select row).First();
        }

        #endregion
    }
}
