using System.Data;

namespace XerParser
{
    public class ActivityDataTable : DataTable
    {

        public ActivityDataTable() : base()
        { }

        public ActivityDataTable(string tableName) : base(tableName)
        {
        }

        protected override Type GetRowType()
        {
            return typeof(ActivityDataRow);
        }

        protected override ActivityDataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new ActivityDataRow(builder);
        }


        

        protected override ActivityDataTable CreateInstance()
        {
            return (ActivityDataTable)Activator.CreateInstance(GetType(), true)!;
        }

    }
}
