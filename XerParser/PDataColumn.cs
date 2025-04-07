using System.Data;

namespace XerParser
{

    public class PDataColumn(string columnName, Type dataType) : DataColumn(columnName, dataType)
    {

        public int ChildTypeId { get; set; }

        public string ChildFieldTypeIdName { get; set; }

        public string ChildFieldValueName { get; set; }

        public string DataRelationName { get; set; }


        public PDataColumn(DataColumn column) : this(column.ColumnName, column.DataType)
        {

        }

        public PDataColumn(string columnName, Type type, string dataRelationName) : this(columnName, type)
        {
            DataRelationName = dataRelationName;
        }
    }
}
