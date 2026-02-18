using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.SupplyChain.Interfaces;
using MatthiasToolbox.Logging;
using MatthiasToolbox.SupplyChain.Database.DataExtensions;
using MatthiasToolbox.SupplyChain.Database;

namespace MatthiasToolbox.SupplyChain
{
    public static class Extensions
    {
        #region IDataExtension

        #region string

        public static string GetString(this IDataExtension source, string name)
        {
            if (!DataExtension.Exists(source.TableName, name))
                throw new InvalidOperationException("There is no DataExtensions with the name " + name + " defined for the table " + source.TableName + ".");

            DataExtension de = Global.ModelDatabase.DataExtensionsByTableName[source.TableName][name];

            if (de.DataType != DataExtensionType.String.ID)
            {
                throw new InvalidOperationException("The type of the data extension property " + name + 
                    " is " + DataExtensionType.Get(de.DataType).Name + " but you attempted to retrieve it as String.");
            }

            if (de.HasMultipleValues)
            {
                throw new InvalidOperationException("The DataExtension " + name + 
                    " may have multiple values. You have to use GetStrings instead of GetString.");
            }

            var q = from row in Global.ModelDatabase.StringDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == source.ID
                    select row;

            if(!q.Any()) 
            {
                q = from row in Global.ModelDatabase.StringDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == 0
                    select row;
            }

            if(!q.Any()) throw new InvalidOperationException("No data found for the data extension " + name + " in " + source.TableName + ".");

            return q.First().Value;
        }

        public static IEnumerable<string> GetStrings(this IDataExtension source, string name)
        {
            if (!DataExtension.Exists(source.TableName, name))
                throw new InvalidOperationException("There is no DataExtensions with the name " + name + " defined for the table " + source.TableName + ".");

            DataExtension de = Global.ModelDatabase.DataExtensionsByTableName[source.TableName][name];

            if (de.DataType != DataExtensionType.String.ID)
            {
                throw new InvalidOperationException("The type of the data extension property " + name +
                    " is " + DataExtensionType.Get(de.DataType).Name + " but you attempted to retrieve it as String.");
            }

            if (!de.HasMultipleValues)
            {
                throw new InvalidOperationException("The DataExtension " + name +
                    " does not have multiple values. You have to use GetString instead of GetStrings.");
            }

            var q = from row in Global.ModelDatabase.StringDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == source.ID
                    select row;

            if (!q.Any())
            {
                q = from row in Global.ModelDatabase.StringDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == 0
                    select row;
            }

            if (!q.Any()) throw new InvalidOperationException("No data found for the data extension " + name + " in " + source.TableName + ".");

            foreach (StringDataExtension sde in q) yield return sde.Value;
        }

        #endregion
        #region bool

        public static bool GetBoolean(this IDataExtension source, string name)
        {
            if (!DataExtension.Exists(source.TableName, name))
                throw new InvalidOperationException("There is no DataExtensions with the name " + name + " defined for the table " + source.TableName + ".");

            DataExtension de = Global.ModelDatabase.DataExtensionsByTableName[source.TableName][name];

            if (de.DataType != DataExtensionType.Boolean.ID)
            {
                throw new InvalidOperationException("The type of the data extension property " + name +
                    " is " + DataExtensionType.Get(de.DataType).Name + " but you attempted to retrieve it as Boolean.");
            }

            if (de.HasMultipleValues)
            {
                throw new InvalidOperationException("The DataExtension " + name +
                    " may have multiple values. You have to use GetBooleans instead of GetBoolean.");
            }

            var q = from row in Global.ModelDatabase.BooleanDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == source.ID
                    select row;

            if (!q.Any())
            {
                q = from row in Global.ModelDatabase.BooleanDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == 0
                    select row;
            }

            if (!q.Any()) throw new InvalidOperationException("No data found for the data extension " + name + " in " + source.TableName + ".");

            return q.First().Value;
        }

        public static IEnumerable<bool> GetBooleans(this IDataExtension source, string name)
        {
            if (!DataExtension.Exists(source.TableName, name))
                throw new InvalidOperationException("There is no DataExtensions with the name " + name + " defined for the table " + source.TableName + ".");

            DataExtension de = Global.ModelDatabase.DataExtensionsByTableName[source.TableName][name];

            if (de.DataType != DataExtensionType.Boolean.ID)
            {
                throw new InvalidOperationException("The type of the data extension property " + name +
                    " is " + DataExtensionType.Get(de.DataType).Name + " but you attempted to retrieve it as Boolean.");
            }

            if (!de.HasMultipleValues)
            {
                throw new InvalidOperationException("The DataExtension " + name +
                    " does not have multiple values. You have to use GetBoolean instead of GetBooleans.");
            }

            var q = from row in Global.ModelDatabase.BooleanDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == source.ID
                    select row;

            if (!q.Any())
            {
                q = from row in Global.ModelDatabase.BooleanDataExtensionTable
                    where row.ExtensionID == de.ID && row.ItemID == 0
                    select row;
            }

            if (!q.Any()) throw new InvalidOperationException("No data found for the data extension " + name + " in " + source.TableName + ".");

            foreach (BooleanDataExtension sde in q) yield return sde.Value;
        }

        #endregion

        #endregion
    }
}
