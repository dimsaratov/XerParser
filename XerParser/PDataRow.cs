using System.Data;

namespace XerParser
{
    public class PDataRow : DataRow
    {
        internal PDataRow(DataRowBuilder builder) : base(builder)
        {
        }

        public new object this[int idx]
        {
            get => Table.Columns[idx] is PDataColumn column ? GetChildValue(column) : base[idx];
            set
            {
                if (Table.Columns[idx] is not PDataColumn)
                {
                    base[idx] = value;
                }
            }
        }


        public new object this[string columnName]
        {
            get => Table.Columns[columnName] is PDataColumn column ? GetChildValue(column) : base[columnName];
            set
            {
                if (Table.Columns[columnName] is not PDataColumn)
                {
                    base[columnName] = value;
                }
            }
        }

        private object GetChildValue(PDataColumn column)
        {
            DataRow[] rows = GetChildRows(column.DataRelationName);


            return GetChildRows(column.DataRelationName).AsEnumerable()
                                                           .Where(r => r.Field<int>(column.ChildFieldTypeIdName) == column.ChildTypeId)
                                                           .FirstOrDefault() is DataRow childRow
                ? childRow[column.ChildFieldValueName]
                : null;
        }
    }
}
