using System.Data;

namespace ExtData
{
    public class PDataRow : System.Data.DataRow
    {
        internal PDataRow(DataRowBuilder builder) : base(builder)
        {
        }

        public new object this[int idx]
        {
            get
            {
                return Table.Columns[idx] is PDataColumn column ?  GetChildValue(column): base[idx];                
            }
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
            get
            {
                return Table.Columns[columnName] is PDataColumn column ?  GetChildValue(column):  base[columnName];                
            }
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
            DataRow[] rows = this.GetChildRows(column.DataRelationName);


            if  (this.GetChildRows(column.DataRelationName).AsEnumerable()
                                                           .Where( r => r.Field<int>(column.ChildFieldTypeIdName) == column.ChildTypeId )
                                                           .FirstOrDefault() is DataRow childRow)
            {
                return childRow[column.ChildFieldValueName];
            }
            else
            {
                return null;
            }
        }
    }
}
