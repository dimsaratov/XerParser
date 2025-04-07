using System.Data;

namespace XerParser
{
    public class PDataTable : DataTable
    {

        public PDataTable() : base()
        { }

        public PDataTable(string tableName) : base(tableName)
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
