using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XerLoader
{
    internal static class Extender
    {
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
