using System.Data;
using System.Diagnostics;

namespace XerParser
{
    public class ActivityDataRow : DataRow
    {
        internal ActivityDataRow(DataRowBuilder builder) : base(builder)
        {
        }

        public new object this[int idx]
        {
            get => Table.Columns[idx] is ChildDataColumn column ? GetChildValue(column) : base[idx];
            set
            {
                if (Table.Columns[idx] is not ChildDataColumn)
                {
                    base[idx] = value;
                }
            }
        }

        private IEnumerable<ChildDataColumn> GetChildDataColumns()
        {
            foreach (DataColumn column in Table.Columns)
            {
                switch (column.GetType().Name)
                {
                    case "ActivityCodeDataColumn":
                        yield return (ActivityCodeDataColumn)column;
                        break;
                    case "UdfDataColumn":
                        yield return (UdfDataColumn)column;
                        break;
                }
            }
        }


        public new object[] ItemArray
        {
            get
            {
                object[] array = base.ItemArray;
                foreach (ChildDataColumn column in GetChildDataColumns())
                {
                    int idx = Table.Columns.IndexOf(column);
                    array[idx] = GetChildValue(column);
                }
                return array;
            }
            set => base.ItemArray = value;
        }


        public new object this[string columnName]
        {
            get => Table.Columns[columnName] is ChildDataColumn column ? GetChildValue(column) : base[columnName];
            set
            {
                if (Table.Columns[columnName] is not ChildDataColumn)
                {
                    base[columnName] = value;
                }
            }
        }

        private object GetChildValue(ChildDataColumn column)
        {
            return GetChildRows(column.DataRelationName).AsEnumerable()
                                                        .Where(r => r.Field<int>(column.ChildFieldTypeIdName) == column.ChildTypeId)
                                                        .FirstOrDefault() is DataRow childRow
                ? childRow[column.ChildFieldValueName]
                : null;
        }
    }
}
