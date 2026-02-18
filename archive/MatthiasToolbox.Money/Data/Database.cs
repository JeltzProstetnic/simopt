using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Reflection;
using System.IO;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Money.Tools;
using System.Globalization;

namespace MatthiasToolbox.Money.Data
{
    public class Database : DataContext, ILogger
    {
        #region cvar

        private static string dbFile;
        private List<PaymentTreeViewItem> treeItems;
        
        #region Group Names

        public static readonly string Other = "Other";
        public static readonly string Income = "Income";
        public static readonly string Housing = "Housing";
        public static readonly string Clothing = "Clothing";
        public static readonly string Luxuries = "Luxuries";
        public static readonly string Nutrition = "Nutrition";
        public static readonly string Equipment = "Equipment";
        public static readonly string Insurance = "Insurance";
        public static readonly string Government = "Government";
        public static readonly string Transportation = "Transportation";

        #endregion
        #region SubGroup Names

        // other
        public static readonly string Health = "Health";
        public static readonly string Credit = "Credit";
        public static readonly string Banking = "Banking";
        public static readonly string Communication = "Communication";

        // income
        public static readonly string Bonus = "Bonus";
        public static readonly string Salary = "Salary";
        public static readonly string Savings = "Savings";

        // government
        public static readonly string Tax = "Tax";
        public static readonly string Fees = "Fees";
        public static readonly string Fines = "Fines";

        // luxuries
        public static readonly string Spa = "Spa";
        public static readonly string Jewelry = "Jewelry";
        public static readonly string Vacation = "Vacation";
        public static readonly string Entertainment = "Entertainment";

        // housing
        public static readonly string Rent = "Rent";
        public static readonly string Energy = "Energy";
        public static readonly string Parking = "Parking";

        // insurance
        public static readonly string Car = "Car";
        public static readonly string Flat = "Flat";
        public static readonly string Travel = "Travel";
        public static readonly string Pension = "Pension";

        // equipment
        public static readonly string Sports = "Sports";
        public static readonly string Business = "Business";
        public static readonly string Maintenance = "Maintenance";
        // public static readonly string Entertainment = "Entertainment";

        // clothing
        public static readonly string Basic = "Basic";
        // public static readonly string Sports = "Sports";
        // public static readonly string Business = "Business";

        // transportation
        // public static readonly string Car = "Car";
        public static readonly string Cab = "Cab";
        public static readonly string Public = "Public";

        // nutrition
        // public static readonly string Basic = "Basic";
        public static readonly string Sweets = "Sweets";
        public static readonly string Beverages = "Beverages";
        public static readonly string Restaurants = "Restaurants";

        #endregion
        #region Tables

        public readonly Table<User> UserTable;
        public readonly Table<Account> AccountTable;
        public readonly Table<Booking> BookingTable;

        public readonly Table<Payment> PaymentTable;
        public readonly Table<PaymentGroup> PaymentGroupTable;
        public readonly Table<PaymentSubGroup> PaymentSubGroupTable;
        public readonly Table<PaymentType> PaymentTypeTable;
        public readonly Table<PeriodicalPayment> PeriodicalPaymentTable;

        public readonly Table<DatabaseLog> DatabaseLogTable;
        public readonly Table<Setting> SettingTable;

        #endregion

        #endregion
        #region prop

        public static Database OpenInstance { get; private set; }
        public static bool Initialized { get; private set; }
        public static long SessionID { get; private set; }

        public static Dictionary<int, Account> AccountsByID { get; private set; }

        #region Payment Groups

        public static Dictionary<string, int> PaymentGroupIDsByName { get; set; }
        public static Dictionary<string, PaymentGroup> PaymentGroupsByName { get; set; }
        public List<PaymentTreeViewItem> PaymentGroupsAndSubGroups { get; private set; }
        
        public IEnumerable<PaymentTreeViewItem> PaymentGroupsTree
        {
            get
            {
                return treeItems;
            }
        }

        #endregion
        #region ILogger

        public bool LoggingEnabled { get; set; }

        #endregion
        #region Settings

        public static Dictionary<string, Type> SettingType { get; private set; }
        public static Dictionary<string, string> SettingValue { get; private set; }
        public static Dictionary<string, Setting> Settings { get; private set; }

        #endregion

        #endregion
        #region ctor

        public Database(string connection)
            : base(connection)
		{
			OpenInstance = this;
            treeItems = new List<PaymentTreeViewItem>();
            PaymentGroupsAndSubGroups = new List<PaymentTreeViewItem>();
            Settings = new Dictionary<string, Setting>();
            SettingType = new Dictionary<string, Type>();
            SettingValue = new Dictionary<string, string>();
            AccountsByID = new Dictionary<int, Account>();
		}

        #endregion
        #region init

        // public static void Reset() { OpenInstance.SubmitChanges(); Database db = new Database(dbFile); }

        public static bool Initialize(string databaseFile, bool resetDB = false)
        {
            if (Initialized) return true;

            String codeBase = Assembly.GetCallingAssembly().CodeBase.ToString();
            String basePath = codeBase.Substring(8, codeBase.LastIndexOf('/') - 8).Replace("/", "\\");
            dbFile = basePath + "\\" + databaseFile;

            try
            {
                Database db = new Database(dbFile);

                if (resetDB && db.DatabaseExists()) 
                    db.DeleteDatabase();
                if (!db.DatabaseExists()) MakeDB(); else CreateLookups();

                if (OpenInstance.DatabaseLogTable.Count<DatabaseLog>() != 0)
                    SessionID = (from log in OpenInstance.DatabaseLogTable select log.SessionID).Max() + 1;
                else
                    SessionID = 0;

                OpenInstance.DatabaseLogTable.DeleteAllOnSubmit((from row in OpenInstance.DatabaseLogTable where row.SessionID < SessionID - 2 select row));
                OpenInstance.SubmitChanges();

                Initialized = true;
                Logger.Add(OpenInstance);
                OpenInstance.LoggingEnabled = true;
                return true;
            }
            catch (Exception e)
            {
                Logger.Log<ERROR>("Error opening DB connection or reading data: ", e);
                return false;
            }
        }

        #endregion
        #region impl

        private static void MakeDB()
        {
            Database.OpenInstance.CreateDatabase();

            OpenInstance.UserTable.InsertOnSubmit(new User("Default User"));
            OpenInstance.UserTable.InsertOnSubmit(new User("All Users"));

            OpenInstance.AccountTable.InsertOnSubmit(new Account(1, "Default Account"));
            OpenInstance.AccountTable.InsertOnSubmit(new Account(2, "All Accounts"));

            OpenInstance.PaymentTypeTable.InsertOnSubmit(new PaymentType("Undefined"));

            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Other));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Income));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Housing));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Clothing));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Luxuries));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Nutrition));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Equipment));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Insurance));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Government));
            OpenInstance.PaymentGroupTable.InsertOnSubmit(new PaymentGroup(Transportation));

            Database.OpenInstance.SubmitChanges();

            PaymentGroupIDsByName = (from row in OpenInstance.PaymentGroupTable select row).ToDictionary(f => f.Name, f => f.ID);
            PaymentGroupsByName = (from row in OpenInstance.PaymentGroupTable select row).ToDictionary(f => f.Name);

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Other]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Health, PaymentGroupIDsByName[Other]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Credit, PaymentGroupIDsByName[Other]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Banking, PaymentGroupIDsByName[Other]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Communication, PaymentGroupIDsByName[Other]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Bonus, PaymentGroupIDsByName[Income]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Salary, PaymentGroupIDsByName[Income]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Savings, PaymentGroupIDsByName[Income]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Income]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Rent, PaymentGroupIDsByName[Housing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Energy, PaymentGroupIDsByName[Housing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Parking, PaymentGroupIDsByName[Housing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Housing]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Basic, PaymentGroupIDsByName[Clothing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Sports, PaymentGroupIDsByName[Clothing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Business, PaymentGroupIDsByName[Clothing]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Clothing]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Spa, PaymentGroupIDsByName[Luxuries]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Jewelry, PaymentGroupIDsByName[Luxuries]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Vacation, PaymentGroupIDsByName[Luxuries]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Entertainment, PaymentGroupIDsByName[Luxuries]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Luxuries]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Basic, PaymentGroupIDsByName[Nutrition]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Sweets, PaymentGroupIDsByName[Nutrition]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Beverages, PaymentGroupIDsByName[Nutrition]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Restaurants, PaymentGroupIDsByName[Nutrition]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Nutrition]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Sports, PaymentGroupIDsByName[Equipment]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Business, PaymentGroupIDsByName[Equipment]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Maintenance, PaymentGroupIDsByName[Equipment]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Entertainment, PaymentGroupIDsByName[Equipment]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Equipment]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Car, PaymentGroupIDsByName[Insurance]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Flat, PaymentGroupIDsByName[Insurance]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Travel, PaymentGroupIDsByName[Insurance]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Pension, PaymentGroupIDsByName[Insurance]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Insurance]));
            
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Tax, PaymentGroupIDsByName[Government]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Fees, PaymentGroupIDsByName[Government]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Fines, PaymentGroupIDsByName[Government]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Government]));

            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Car, PaymentGroupIDsByName[Transportation]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Cab, PaymentGroupIDsByName[Transportation]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Public, PaymentGroupIDsByName[Transportation]));
            OpenInstance.PaymentSubGroupTable.InsertOnSubmit(new PaymentSubGroup(Other, PaymentGroupIDsByName[Transportation]));

            Database.OpenInstance.SubmitChanges();

            foreach (PaymentGroup pg in PaymentGroupsByName.Values)
            {
                PaymentTreeViewItem ti = new PaymentTreeViewItem(pg);
                OpenInstance.treeItems.Add(ti);
                OpenInstance.PaymentGroupsAndSubGroups.Add(ti);
                OpenInstance.PaymentGroupsAndSubGroups.AddRange(ti.Children);
            }

            CreateMoreLookups();
        }

        private static void CreateLookups() 
        {
            PaymentGroupIDsByName = (from row in OpenInstance.PaymentGroupTable select row).ToDictionary(f => f.Name, f => f.ID);
            PaymentGroupsByName = (from row in OpenInstance.PaymentGroupTable select row).ToDictionary(f => f.Name);
            foreach (PaymentGroup pg in PaymentGroupsByName.Values)
            {
                PaymentTreeViewItem ti = new PaymentTreeViewItem(pg);
                OpenInstance.treeItems.Add(ti);
                OpenInstance.PaymentGroupsAndSubGroups.Add(ti);
                OpenInstance.PaymentGroupsAndSubGroups.AddRange(ti.Children);
            }

            CreateMoreLookups();
        }

        private static void CreateMoreLookups()
        {
            if ((from row in OpenInstance.SettingTable select row).Any()) // Count() != 0
            {
                List<Setting> settings = (from row in OpenInstance.SettingTable select row).ToList();
                foreach (Setting setting in settings)
                {
                    SettingValue[setting.Name] = setting.SettingData;
                    SettingType[setting.Name] = Type.GetType(setting.DataClass);
                    Settings[setting.Name] = setting;
                }
            }
            List<Account> accounts = (from row in OpenInstance.AccountTable select row).ToList();
            foreach (Account account in accounts)
            {
                AccountsByID[account.ID] = account;
            }
        }

        #region Import and Export

        // TODO: use MD5 hashes instead of line.GetHashCode();

        /// <summary>
        /// Kontonummer;Auszugsnummer;Buchungsdatum;Valutadatum;Umsatzzeit;Kundendaten;Waehrung;Betrag;Buchungstext;Umsatztext
        /// </summary>
        /// <param name="source"></param>
        /// <param name="accountID"></param>
        /// <returns>a list of bookings which were already imported once</returns>
        public List<Booking> ImportHypoCSV(FileInfo source, int accountID = 0) 
        {
            CultureInfo cult = new CultureInfo("de-DE");
            List<Booking> bookings = new List<Booking>();
            List<Booking> alreadyAdded = new List<Booking>();
            int accID = accountID;
            bool autoDetectAccount = (accountID == 0);
            // StreamReader sr = source.OpenText();
            // StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.ASCII);
            // StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.UTF8);
            StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.UTF7);
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.Unicode);
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(1252));
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(437));
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(850));

            string fileContents = sr.ReadToEnd();
            List<string> lines = fileContents.Split('\n').ToList();
            if (lines[0].StartsWith("Kontonummer")) lines.RemoveAt(0);
            foreach (string line in lines) 
            {
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        int hash = line.GetHashCode();
                        string[] data = line.Split(';');

                        if (autoDetectAccount)
                        {
                            string accountNumber = data[0];
                            int c = (from acc in AccountTable where acc.AccountNumber.Trim() == accountNumber.Trim() select acc.ID).Count();
                            if (c == 0)
                            {
                                AccountTable.InsertOnSubmit(new Account(1, "Unknown", data[0]));
                                Database.OpenInstance.SubmitChanges();
                            }
                            accID = (from acc in AccountTable where acc.AccountNumber == accountNumber select acc.ID).First<int>();
                            autoDetectAccount = false;
                        }

                        int accountStatementID = int.Parse(data[1]);
                        DateTime bookingDate = DateTime.Parse(data[2]);
                        DateTime valutaDate = DateTime.Parse(data[3]);
                        string tmp = data[4].Substring(0, 10) + " " + data[4].Substring(11, 8).Replace(".", ":");
                        DateTime transferTime = DateTime.Parse(tmp);
                        string customerData = data[5].Trim('\"', '\\').Trim();
                        string currency = data[6];
                        float amount = float.Parse(data[7], cult);
                        string bookingText = data[8].Trim().Trim('\"', '\\').Trim();
                        string transferText = "";
                        if (data.GetUpperBound(0) >= 9)
                        {
                            for (int i = 9; i <= data.GetUpperBound(0); i += 1)
                            {
                                transferText += data[i].Trim().Trim('\"', '\\').Trim();
                                if (i < data.GetUpperBound(0)) transferText += "\n";
                            }
                        }
                        //if (data.GetUpperBound(0) == 9)
                        //    transferText = data[9].Trim().Trim('\"', '\\').Trim();
                        //else if (data.GetUpperBound(0) == 10)
                        //    transferText = data[9].Trim().Trim('\"', '\\').Trim() + " " + data[10].Trim().Trim('\"', '\\').Trim();
                        Booking booking = new Booking(accID, hash, accountStatementID, bookingDate, valutaDate, transferTime, customerData, currency, amount, bookingText, transferText);
                        if ((from bk in BookingTable where bk.Hash == hash select bk).Count() == 0)
                        {
                            bookings.Add(booking);
                        }
                        else
                        {
                            this.Log<WARN>("The followin booking was already imported: " + line);
                            alreadyAdded.Add(booking);
                        }
                    }
                    catch (Exception e)
                    {
                        this.Log("The line \"" + line + "\" was not imported due to an exception: ", e);
                    }
                }
            }

            foreach (Booking b in bookings) BookingTable.InsertAllOnSubmit(bookings);
            SubmitChanges();
            this.Log<INFO>("Successfully imported " + bookings.Count.ToString() + 
                " of " + (lines.Count - 1 + alreadyAdded.Count).ToString() + 
                " items from <" + source.Name + ">.");
            return alreadyAdded;
        }

        /// <summary>
        /// Umsatz vom;Buchung;Rechnungstext;Fremdwaehrung;Fremdwaehrungsbetrag;Fremdwaehrungskurs;Manipulationsbetrag;Gesamt;Branche
        /// </summary>
        /// <param name="source"></param>
        /// <param name="accountID"></param>
        /// <returns>a list of bookings which were already imported once</returns>
        public List<Booking> ImportMastercardCSV(FileInfo source, int accountID)
        {
            CultureInfo cult = new CultureInfo("de-DE");
            List<Booking> bookings = new List<Booking>();
            List<Booking> alreadyAdded = new List<Booking>();
            StreamReader sr = source.OpenText();
            // StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.ASCII);
            // StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.UTF8);
            // StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.UTF7);
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.Unicode);
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(1252));
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(437));
            //StreamReader sr = new StreamReader(source.ToString(), System.Text.Encoding.GetEncoding(850));

            string fileContents = sr.ReadToEnd();
            List<string> lines = fileContents.Split('\n').ToList();
            if (lines[0].StartsWith("Umsatz")) lines.RemoveAt(0);
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        int hash = line.GetHashCode();
                        string[] data = line.Split(';');

                        int accountStatementID = -1;
                        DateTime bookingDate = DateTime.Parse(data[1], cult);
                        DateTime valutaDate = DateTime.Parse(data[0], cult);
                        DateTime transferTime = bookingDate;
                        string customerData = "";
                        if (!string.IsNullOrEmpty(data[4]))
                        {
                            customerData = data[3] + " " + data[4];
                            if (!string.IsNullOrEmpty(data[5]))
                                customerData += "; Kurs " + data[5];
                            if (!string.IsNullOrEmpty(data[6]))
                                customerData += "; Gebühr " + data[6];
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(data[6]))
                                customerData = "Gebühr " + data[6];
                        }
                        string currency = "EUR";
                        float amount = float.Parse(data[7], cult);
                        string transferText = data[2].Trim('\"', '\\').Trim();
                        string bookingText = data[8];
                        Booking booking = new Booking(accountID, hash, accountStatementID, bookingDate, valutaDate, transferTime, customerData, currency, amount, bookingText, transferText);
                        if ((from bk in BookingTable where bk.Hash == hash select bk).Count() == 0)
                        {
                            bookings.Add(booking);
                        }
                        else
                        {
                            this.Log<WARN>("The followin booking was already imported: " + line);
                            alreadyAdded.Add(booking);
                        }
                    }
                    catch (Exception e)
                    {
                        this.Log("The line \"" + line + "\" was not imported due to an exception: ", e);
                    }
                }
            }

            foreach (Booking b in bookings) BookingTable.InsertAllOnSubmit(bookings);
            SubmitChanges();
            this.Log<INFO>("Successfully imported " + bookings.Count.ToString() +
                " of " + (lines.Count - 1 + alreadyAdded.Count).ToString() +
                " items from <" + source.Name + ">.");
            return alreadyAdded;
        }

        /// <summary>
        /// Kontonummer;Auszugsnummer;Buchungsdatum;Valutadatum;Umsatzzeit;Kundendaten;Waehrung;Betrag;Buchungstext;Umsatztext
        /// </summary>
        /// <param name="source"></param>
        /// <param name="accountID"></param>
        /// <returns>a list of bookings which were already imported once</returns>
        public void ImportPaymentCSV(FileInfo source)
        {
            int n = 0;
            StreamReader sr = source.OpenText();

            string fileContents = sr.ReadToEnd();
            List<string> lines = fileContents.Split('\n').ToList();
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        string[] data = line.Trim().Split(';');
                        
                        Payment p = new Payment(float.Parse(data[0]), DateTime.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]),
                            int.Parse(data[4]), int.Parse(data[5]), int.Parse(data[6]), int.Parse(data[7]), bool.Parse(data[8]), data[9].Replace("|||",";"));
                        PaymentTable.InsertOnSubmit(p);
                        n += 1;
                    }
                    catch (Exception e)
                    {
                        this.Log("The line \"" + line + "\" was not imported due to an exception: ", e);
                    }
                }
            }
            SubmitChanges();
            this.Log<INFO>("Successfully imported " + n.ToString() + " items from <" + source.Name + ">.");
        }

        public void ExportPaymentCSV(FileInfo target)
        {
            int n = 0;
            StreamWriter sw = target.CreateText();

            string line;
            // TODO: save booking hash instead of id but exclude ids from hash
            foreach (Payment p in OpenInstance.PaymentTable)
            {
                if (string.IsNullOrEmpty(p.Comment))
                {
                    line = p.Amount.ToString() + ";" + new DateTime(p.TransactionDateTicks).ToString() + ";" + p.PayedByUserID.ToString() + ";" +
                            p.GroupID.ToString() + ";" + p.SubGroupID.ToString() + ";" + p.TypeID.ToString() + ";" + p.BookingID.ToString() + ";" +
                            p.PayedFromAccountID.ToString() + ";" + p.TaxRefundPossible.ToString() + ";";
                }
                else
                {
                    line = p.Amount.ToString() + ";" + new DateTime(p.TransactionDateTicks).ToString() + ";" + p.PayedByUserID.ToString() + ";" +
                            p.GroupID.ToString() + ";" + p.SubGroupID.ToString() + ";" + p.TypeID.ToString() + ";" + p.BookingID.ToString() + ";" +
                            p.PayedFromAccountID.ToString() + ";" + p.TaxRefundPossible.ToString() + ";" + p.Comment.Replace(";", "|||");
                }
                try
                {
                    sw.WriteLine(line);
                }
                catch (Exception e)
                {
                    this.Log("The line \"" + line + "\" was not exported due to an exception: ", e);
                }
            }
            sw.Flush();
            sw.Close();
            this.Log<INFO>("Successfully exported " + n.ToString() + " items to <" + target.Name + ">.");
        }

        #endregion
        #region ILogger

        void ILogger.Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (data.ContainsKey("LogToDB") && data["LogToDB"].GetType() == typeof(bool) && (bool)data["LogToDB"] == false) 
                return;

            int severity = 0;
            if (data.ContainsKey("Severity") && data["Severity"].GetType() == typeof(int)) 
                severity = (int)data["Severity"];
            
            DatabaseLog dbl = new DatabaseLog(timeStamp.Ticks, severity, messageClass.Name, message, sender.GetType().FullName);
            
            try
            {
                DatabaseLogTable.InsertOnSubmit(dbl);
            }
            catch(Exception ex)
            {
                this.Log<ERROR>("Database logging error: ", ex, LogToDB => false);
            }
        }

        public void ShutdownLogger()
        {
            LoggingEnabled = false;
            try
            {
                OpenInstance.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
            }
        }

        #endregion

        #endregion
    }
}
