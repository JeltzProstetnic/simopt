using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblSettings")]
    public class Setting
    {
        #region prop

        public Type DataTyp
        {
            get
            {
                return Type.GetType(DataClass);
            }
            set
            {
                DataClass = value.FullName;
            }
        }

        #endregion
        #region ctor

        public Setting() { }

        public Setting(string name, Type type, string value) 
        { 
            Name = name;
            DataClass = type.FullName;
            SettingData = value;
        }

        #endregion
        #region data

#pragma warning disable 0649
        private int id;
#pragma warning restore 0649

        [Column(Storage = "id",
                AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID
        {
            get { return id; }
        }

        [Column]
        public string Name { get; set; }

        [Column]
        public string SettingData { get; set; }

        [Column]
        public string DataClass { get; set; }

        #endregion
        #region impl

        public static void Set(string name, object value)
        {
            string val;
            if (value == null) val = "{NULL}";
            else val = value.ToString();

            if (Exists(name))
            {
                Database.Settings[name].SettingData = val;
                Database.SettingValue[name] = val;
                Database.OpenInstance.SubmitChanges();
            }
            else
            {
                Setting s = new Setting(name, value.GetType(), val);
                Database.OpenInstance.SettingTable.InsertOnSubmit(s);
                Database.OpenInstance.SubmitChanges();
                Database.SettingType[name] = value.GetType();
                Database.SettingValue[name] = val;
                Database.Settings[name] = s;
            }
        }

        public static void Set<T>(string name, T value)
        {
            if (value == null) Set<T>(name, "{NULL}");
            else Set<T>(name, value.ToString());
        }

        public static void Set<T>(string name, string value)
        {
            string val;
            if (value == null) val = "{NULL}";
            else val = value.ToString();

            if (Exists(name))
            {
                Database.Settings[name].SettingData = val;
                Database.SettingValue[name] = val;
                Database.OpenInstance.SubmitChanges();
            }
            else
            {
                Setting s = new Setting(name, typeof(T), val);
                Database.OpenInstance.SettingTable.InsertOnSubmit(s);
                Database.OpenInstance.SubmitChanges();
                Database.SettingType[name] = typeof(T);
                Database.SettingValue[name] = val;
                Database.Settings[name] = s;
            }
        }

        public static string Get(string name)
        {
            if (!Exists(name))
            {
                Logger.Log<ERROR>("Setting " + name + " not found. Returning null value.");
                return null;
            }
            if (Database.SettingValue[name] == "{NULL}") return null;
            return Database.SettingValue[name];
        }

        public static T Get<T>(string name)
        {
            return ConvertSetting<T>(Database.SettingValue[name]);
        }

        /// <summary>
        /// defaultValue will be returned if the actual value equals default(T)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(string name, T defaultValue)
        {
            if (!Database.SettingValue.ContainsKey(name)) return defaultValue;
            T result = ConvertSetting<T>(Database.SettingValue[name]);
            if (result.Equals(default(T))) return defaultValue;
            return result;
        }

        /// <summary>
        /// replacementValue will be returned if value equals unacceptableValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="unacceptableValue"></param>
        /// <param name="replacementValue"></param>
        /// <returns></returns>
        public static T Get<T>(string name, T unacceptableValue, T replacementValue)
        {
            T result = ConvertSetting<T>(Database.SettingValue[name]);
            if (result.Equals(unacceptableValue)) return replacementValue;
            return result;
        }

        public static bool Exists(string name)
        {
            return Database.SettingValue.ContainsKey(name);
        }

        public static Type TypeOf(string name)
        {
            return Database.SettingType[name];
        }

        private static T ConvertSetting<T>(string value)
        {
            if (value == "{NULL}") return default(T); // ... and hope this is actually null

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Parse(value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.Parse(value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(value);
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(value);
            }
            else if (typeof(T) == typeof(char))
            {
                return (T)(object)char.Parse(value);
            }
            else
            {
                Logger.Log<ERROR>("Setting type " + typeof(T).FullName + " cannot be converted. Returning default(T).");
                return default(T);
            }
        }

        #endregion
    }
}
