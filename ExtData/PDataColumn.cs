using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtData
{



    public class PDataColumn(string columnName, Type dataType) : System.Data.DataColumn(columnName, dataType)
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
