using System.ComponentModel;
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

        public static void ReplaceTableTask(this DataSet ds)
        {
            DataTable table = ds.Tables["TASK"];

            ActivityDataTable task = new(table.TableName);
            task.Columns.AddRange([.. table.Columns.Cast<DataColumn>().Select(c => new DataColumn(c.ColumnName, c.DataType))]);

            DataSet dataSet = table.DataSet;
            DataRelation[] dr = [.. dataSet.Relations.Cast<DataRelation>()];
            List<DataColumn> pk = [];
            foreach (DataColumn column in table.PrimaryKey)
            {
                pk.Add(task.Columns[column.ColumnName]);
            }
            task.PrimaryKey = [.. pk];

            List<Tuple<string, string, string, string[], string[]>> rels = [];

            foreach (DataRelation r in dr)
            {
                Tuple<string, string, string, string[], string[]> t =
                                      new(r.RelationName,
                                          r.ParentTable.TableName,
                                          r.ChildTable.TableName,
                                          [.. r.ParentColumns.Cast<DataColumn>().Select(c => c.ColumnName)],
                                          [.. r.ChildColumns.Cast<DataColumn>().Select(c => c.ColumnName)]);
                rels.Add(t);
                dataSet.Relations.Remove(r);
            }

            DataTable[] tables = [.. dataSet.Tables.Cast<DataTable>()];
            int idx = dataSet.Tables.IndexOf(table);
            foreach (DataTable dt in tables)
            {
                dataSet.Tables.Remove(dt);
            }
            tables[idx] = task;
            dataSet.Tables.AddRange([.. tables.OrderBy(t => t.TableName)]);

            foreach (Tuple<string, string, string, string[], string[]> t in rels)
            {
                DataRelation data = new(t.Item1,
                                        dataSet.Tables[t.Item2].Columns[t.Item4[0]],
                                        dataSet.Tables[t.Item3].Columns[t.Item5[0]],
                                        false);
                dataSet.Relations.Add(data);
            }
            ds.AcceptChanges();
        }
    }
}
