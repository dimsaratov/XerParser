using System.Data;

namespace XerParser
{

    public class ChildDataColumn(string columnName, Type dataType) : DataColumn(columnName, dataType)
    {

        public int ChildTypeId { get; set; }

        public virtual string ChildFieldTypeIdName { get; set; }

        public string ChildFieldValueName { get; set; }

        public virtual string DataRelationName { get; }

        public ChildDataColumn(DataColumn column) : this(column.ColumnName, column.DataType)
        {

        }

        public ChildDataColumn(string columnName, Type type, string dataRelationName) : this(columnName, type)
        {
            DataRelationName = dataRelationName;
        }
    }
}
