using System.Data;
using System.Reflection;

namespace XerParser
{

    public class ChildDataColumn : DataColumn
    {

        private readonly MethodInfo _getDataRow;

        public int ChildTypeId { get; set; }

        public virtual string ChildFieldTypeIdName { get; set; }

        public string ChildFieldValueName { get; set; }

        public virtual string DataRelationName { get; }


        public ChildDataColumn() : base()
        {
            _getDataRow = typeof(DataColumn).GetMethod("GetDataRow", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(int)]);
        }

        public ChildDataColumn(string columnName, Type type) : this(columnName, type, null)
        {
        }

        public ChildDataColumn(string columnName, Type type, string dataRelationName) : this()
        {
            ColumnName = columnName;
            DataType = type;
            DataRelationName = dataRelationName;
        }

        internal virtual object this[int record]
        {
            get => GetChild(record) is DataRow child ? child[ChildFieldValueName] : DBNull.Value;
            set
            {
                if (GetChild(record) is DataRow child)
                {
                    child[ChildFieldValueName] = value;
                }
            }
        }

        private DataRow GetChild(int record)
        {
            if (_getDataRow.Invoke(this, [record]) is DataRow row)
            {
                if ((from c in row.GetChildRows(DataRelationName)
                     where c.Field<int>(ChildFieldTypeIdName) == ChildTypeId
                     select c).FirstOrDefault() is DataRow child)
                {
                    return child;
                }
            }
            return null;
        }
    }
}

