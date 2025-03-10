using System.Data;

namespace XerParser
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
