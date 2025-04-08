using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XerParser
{
    public class ActivityDataView : System.Data.DataView
    {

        public ActivityDataView(ActivityDataTable table) : base()
        {
            this.Table = table;
        }

        public ActivityDataView(ActivityDataTable table, string rowFilter, string sort, DataViewRowState rowState) : this(table)
        {
            RowFilter = rowFilter;
            Sort = sort;
            RowStateFilter = rowState;
        }

        public override string RowFilter
        {
            get => base.RowFilter;
            set => base.RowFilter = value;
        }

        public new ActivityDataTable Table
        {
            get => (ActivityDataTable)base.Table;
            set
            {
                if (value.GetType().Equals(typeof(ActivityDataTable)))
                {
                    base.Table = value;
                }
            }
        }
    }
}
