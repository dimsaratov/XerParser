﻿using System.ComponentModel;
using System.Data;

namespace XerParser
{
    /// <summary>
    /// 
    /// </summary>
    public enum NumberDecimalSeparator
    {
        /// <summary>
        /// Point separator
        /// </summary>
        [Description("Точка")]
        Point,
        /// <summary>
        /// Comma separator
        /// </summary>
        [Description("Запятая")]
        Comma
    }


    internal static class Extender
    {

        internal static string TryGet(this string[] records, int index)
        {
            return index == -1 ? string.Empty : records[index];
        }

        public static bool TryGetTable(this DataSet dataSet, string tableName, out DataTable table)
        {
            table = null;
            if (dataSet.Tables.Contains(tableName))
            {
                table = dataSet.Tables[tableName];
                return true;
            }
            return false;
        }
    }
}
