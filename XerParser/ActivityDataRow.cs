using System.Data;
using System.Reflection;

namespace XerParser
{
    public class ActivityDataRow : DataRow
    {
        internal ActivityDataRow(DataRowBuilder builder) : base(builder)
        {
        }

        public new object[] ItemArray
        {
            get
            {
                object[] array = new object[Table.Columns.Count];
                int record = Table.Rows.IndexOf(this);
                for (int i = 0; i < Table.Columns.Count; i++)
                {
                    array[i] = GetItem(i, record);
                }
                return array;
            }
            set => base.ItemArray = value;
        }

#pragma warning disable CA1859 // Используйте конкретные типы, когда это возможно, для повышения производительности
        private object GetItem(int idx, int record)
#pragma warning restore CA1859 // Используйте конкретные типы, когда это возможно, для повышения производительности
        {
            dynamic column = Table.Columns[idx];
            Type type = column.GetType();
            if (type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic) is MethodInfo ps)
            {
#pragma warning disable IDE0300 // Упростите инициализацию коллекции
                return ps.Invoke(column, new object[] { record });
#pragma warning restore IDE0300 // Упростите инициализацию коллекции
            }
            return DBNull.Value;
        }

#pragma warning disable CA1859 // Используйте конкретные типы, когда это возможно, для повышения производительности
        private object GetItem(string columnName, int record)
#pragma warning restore CA1859 // Используйте конкретные типы, когда это возможно, для повышения производительности
        {
            dynamic column = Table.Columns[columnName];
            Type type = column.GetType();
            if (type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic) is MethodInfo ps)
            {
#pragma warning disable IDE0300 // Упростите инициализацию коллекции
                return ps.Invoke(column, new object[] { record });
#pragma warning restore IDE0300 // Упростите инициализацию коллекции
            }
            return DBNull.Value;
        }


        public new object this[string columnName]
        {
            get
            {
                int record = Table.Rows.IndexOf(this);
                return GetItem(columnName, record);
            }
            set
            {
                int idx = Table.Columns.IndexOf(columnName);
                this[idx] = value;
            }
        }


        public new object this[int idx]
        {
            get
            {
                int record = Table.Rows.IndexOf(this);
                return GetItem(idx, record);
            }
            set
            {
                if (Table.Columns[idx] is ChildDataColumn col && !col.ReadOnly)
                {
                    dynamic column = Table.Columns[idx];
                    column[idx] = value;
                }
                else
                {
                    base[idx] = value;
                }
            }
        }

        public Type GetDataType(string dataPropertyItem)
        {
            return Table?.Columns[dataPropertyItem]?.DataType;
        }
    }
}
