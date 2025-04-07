using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtData
{
    public class PDataTable: DataTable
    {

        public PDataTable() :base() 
        { }

        public PDataTable(string tableName): base(tableName)
        {
        }

        protected override Type GetRowType()
        {
            return typeof(PDataRow);
        }

        protected override PDataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new PDataRow(builder);
        }

    
    }
}
