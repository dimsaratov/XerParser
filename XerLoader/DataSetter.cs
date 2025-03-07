using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XerLoader.XerElement;

namespace XerLoader
{
    internal class DataSetter(DataColumn column)
    {
        private readonly DataColumn column = column;
        private readonly ValueParse parser = column.DataType.Name switch
        {
            "DateTime" => new ValueParse(DateTimeParse),
            "Int32" => new ValueParse(IntParse),
            "Decimal" => new ValueParse(DecimalParse),
            _ => new ValueParse(StringParse),
        };

        public string Name { get => column.ColumnName; }

        public Type Type 
        { get => column.DataType; }

        public ValueParse ValueParse { get => parser; }      

        public DataColumn Column
        {
            get => column;
        }

        public

        static object DecimalParse(string value)
        {
            if (value.Length > 0 && decimal.TryParse(value, NumberStyles.Float, XerParser.NumberFormat, out decimal n))
                return n;
            else 
                return DBNull.Value;
        }

        static object DateTimeParse(string value)
        {
            if (value.Length > 0 && DateTime.TryParse(value, out DateTime d))
                return d;
            else
                return DBNull.Value;
        }

        static string StringParse(string value)
        {
            return value;
        }
        static object IntParse(string value)
        {       
            if (value.Length > 0 && int.TryParse(value, out int i))
                return i;
            else
               return DBNull.Value;
        }

    }
}
