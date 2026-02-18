using System;
using System.Collections.Generic;

namespace MatthiasToolbox.SupplyChain.Database
{
    public class DataExtensionType
    {
        private static Dictionary<int, DataExtensionType> typesByID;
        private static Dictionary<string, DataExtensionType> typesByName;

        public static DataExtensionType Get(int id) { return typesByID[id]; }
        public static DataExtensionType Get(string name) { return typesByName[name]; }

        public static readonly DataExtensionType Undefined = new DataExtensionType(0, "Undefined");
        public static readonly DataExtensionType Boolean = new DataExtensionType(1, "Boolean");
        public static readonly DataExtensionType String = new DataExtensionType(2, "String");
        public static readonly DataExtensionType Integer = new DataExtensionType(3, "Integer");
        public static readonly DataExtensionType Long = new DataExtensionType(4, "Long");
        public static readonly DataExtensionType Float = new DataExtensionType(5, "Float");
        public static readonly DataExtensionType Double = new DataExtensionType(6, "Double");
        public static readonly DataExtensionType Decimal = new DataExtensionType(7, "Decimal");

        public int ID { get; private set; }
        public string Name { get; private set; }

        static DataExtensionType()
        {
            typesByID = new Dictionary<int, DataExtensionType>();
            typesByName = new Dictionary<string, DataExtensionType>();
        }

        private DataExtensionType(int id, string name)
        {
            this.ID = id;
            this.Name = name;
            typesByID[id] = this;
            typesByName[name] = this;
        }
    }
}