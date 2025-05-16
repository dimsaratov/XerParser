using System.ComponentModel;

namespace XerParser.Enums
{
    public enum PrimaveraVersion
    {
        [Description("6.2")]
        Primavera62,

        [Description("19.2")]
        Primavera192,
    }

    public static class Enums
    {
        /// <summary>
        /// Получить атрибут "Описание"
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            System.Reflection.FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes != null && attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
