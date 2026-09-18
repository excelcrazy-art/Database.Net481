using System;
using System.Globalization;

namespace Database.Net481.ORM.Mapping
{
    /// <summary>
    /// 데이터베이스 값과 .NET 값 사이의 변환을 담당합니다.
    /// </summary>
    public static class DbValueConverter
    {
        /// <summary>
        /// 데이터베이스 값을 지정한 .NET 타입으로 변환합니다.
        /// </summary>
        /// <param name="value">데이터베이스에서 읽은 값입니다.</param>
        /// <param name="targetType">변환할 .NET 타입입니다.</param>
        /// <returns>변환된 값입니다.</returns>
        public static object ConvertTo(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            // DB의 NULL
            if (value == null || value == DBNull.Value)
            {
                if (IsNullable(targetType))
                    return null;

                return GetDefaultValue(targetType);
            }

            // Nullable<T>인 경우 실제 타입으로 변환
            Type underlyingType = Nullable.GetUnderlyingType(targetType);

            if (underlyingType != null)
                targetType = underlyingType;

            // 이미 원하는 타입인 경우
            if (targetType.IsInstanceOfType(value))
                return value;

            // Enum
            if (targetType.IsEnum)
                return ConvertToEnum(value, targetType);

            // Guid
            if (targetType == typeof(Guid))
                return ConvertToGuid(value);

            // Boolean
            if (targetType == typeof(bool))
                return ConvertToBoolean(value);

            // DateTime
            if (targetType == typeof(DateTime))
                return ConvertToDateTime(value);

            // DateTimeOffset
            if (targetType == typeof(DateTimeOffset))
                return ConvertToDateTimeOffset(value);

            // Char
            if (targetType == typeof(char))
                return ConvertToChar(value);

            // 일반적인 기본 타입 변환
            return Convert.ChangeType(
                value,
                targetType,
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// .NET 값을 데이터베이스에 전달할 수 있는 값으로 변환합니다.
        /// </summary>
        /// <param name="value">.NET 값입니다.</param>
        /// <returns>데이터베이스에 전달할 값입니다.</returns>
        public static object ConvertToDbValue(object value)
        {
            if (value == null)
                return DBNull.Value;

            if (value == DBNull.Value)
                return DBNull.Value;

            Type valueType = value.GetType();

            // Nullable<T>는 실제 값의 타입으로 전달됩니다.
            Type underlyingType = Nullable.GetUnderlyingType(valueType);

            if (underlyingType != null)
                valueType = underlyingType;

            // Enum은 기본 숫자 타입으로 변환
            if (valueType.IsEnum)
                return Convert.ChangeType(
                    value,
                    Enum.GetUnderlyingType(valueType),
                    CultureInfo.InvariantCulture);

            // Guid는 문자열로 변환하지 않습니다.
            // 각 DB Provider가 Guid를 지원할 수 있도록 Guid 자체를 전달합니다.
            if (valueType == typeof(Guid))
                return value;

            // DateTime, DateTimeOffset, Boolean 등은
            // Provider가 적절한 DB 타입으로 처리하도록 그대로 전달합니다.
            return value;
        }

        /// <summary>
        /// 지정한 타입이 Nullable 타입인지 확인합니다.
        /// </summary>
        public static bool IsNullable(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return !type.IsValueType ||
                   Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// 지정한 타입의 기본값을 반환합니다.
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType)
                return null;

            Type underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
                return null;

            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Enum 타입으로 변환합니다.
        /// </summary>
        private static object ConvertToEnum(object value, Type enumType)
        {
            if (value is string)
            {
                string text = Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture);

                return Enum.Parse(
                    enumType,
                    text,
                    true);
            }

            Type underlyingType = Enum.GetUnderlyingType(enumType);

            object numericValue = Convert.ChangeType(
                value,
                underlyingType,
                CultureInfo.InvariantCulture);

            return Enum.ToObject(enumType, numericValue);
        }

        /// <summary>
        /// Guid 타입으로 변환합니다.
        /// </summary>
        private static Guid ConvertToGuid(object value)
        {
            if (value is Guid)
                return (Guid)value;

            if (value is byte[])
                return new Guid((byte[])value);

            string text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture);

            return Guid.Parse(text);
        }

        /// <summary>
        /// Boolean 타입으로 변환합니다.
        /// </summary>
        private static bool ConvertToBoolean(object value)
        {
            if (value is bool)
                return (bool)value;

            if (value is string)
            {
                string text = ((string)value).Trim();

                if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "n", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (IsNumericType(value.GetType()))
                return Convert.ToDecimal(
                           value,
                           CultureInfo.InvariantCulture) != 0m;

            return Convert.ToBoolean(
                value,
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// DateTime 타입으로 변환합니다.
        /// </summary>
        private static DateTime ConvertToDateTime(object value)
        {
            if (value is DateTime)
                return (DateTime)value;

            if (value is DateTimeOffset)
                return ((DateTimeOffset)value).DateTime;

            return Convert.ToDateTime(
                value,
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// DateTimeOffset 타입으로 변환합니다.
        /// </summary>
        private static DateTimeOffset ConvertToDateTimeOffset(object value)
        {
            if (value is DateTimeOffset)
                return (DateTimeOffset)value;

            if (value is DateTime)
                return new DateTimeOffset((DateTime)value);

            return DateTimeOffset.Parse(
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Char 타입으로 변환합니다.
        /// </summary>
        private static char ConvertToChar(object value)
        {
            if (value is char)
                return (char)value;

            string text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(text))
                throw new InvalidCastException(
                    "문자열을 Char로 변환할 수 없습니다.");

            return text[0];
        }

        /// <summary>
        /// 지정한 타입이 숫자 타입인지 확인합니다.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long) ||
                   type == typeof(ulong) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}