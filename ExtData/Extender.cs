using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ExtData
{
    public static class Extender
    {
        public static void ReplaceTableTask(this DataSet ds)
        {
            DataTable table = ds.Tables["TASK"];

            PDataTable task = new(table.TableName);
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

            DataTable[] tables =[.. dataSet.Tables.Cast<DataTable>()];
            int idx = dataSet.Tables.IndexOf(table);         
            foreach(DataTable dt in tables)
            {
                dataSet.Tables.Remove(dt);
            }
            tables[idx] = task;
            dataSet.Tables.AddRange([.. tables.OrderBy(t =>t.TableName)]);

            foreach (var t in rels)
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
