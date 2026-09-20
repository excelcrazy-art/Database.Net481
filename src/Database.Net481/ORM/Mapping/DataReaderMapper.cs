using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.ORM.Mapping
{
    /// <summary>
    /// IDataReader의 데이터를 Entity 객체로 매핑합니다.
    /// </summary>
    public static class DataReaderMapper
    {
        /// <summary>
        /// IDataReader의 현재 행을 Entity 객체로 변환합니다.
        /// </summary>
        /// <typeparam name="T">Entity 타입입니다.</typeparam>
        /// <param name="reader">데이터를 읽을 IDataReader입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        /// <returns>매핑된 Entity 객체입니다.</returns>
        public static T Map<T>(
            IDataReader reader,
            EntityMetadata metadata)
            where T : new()
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            T entity = new T();

            MapToEntity(
                reader,
                entity,
                metadata);

            return entity;
        }

        /// <summary>
        /// IDataReader의 현재 행을 기존 Entity 객체에 매핑합니다.
        /// </summary>
        /// <param name="reader">데이터를 읽을 IDataReader입니다.</param>
        /// <param name="entity">매핑할 Entity 객체입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        public static void MapToEntity(
            IDataReader reader,
            object entity,
            EntityMetadata metadata)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            Dictionary<string, int> columnOrdinals =
                CreateColumnOrdinals(reader);

            foreach (ColumnMetadata column in metadata.Columns)
            {
                if (column == null ||
                    column.PropertyInfo == null)
                {
                    continue;
                }

                int ordinal;

                if (!columnOrdinals.TryGetValue(
                        column.ColumnName,
                        out ordinal))
                {
                    continue;
                }

                if (reader.IsDBNull(ordinal))
                {
                    SetNullValue(
                        entity,
                        column.PropertyInfo);

                    continue;
                }

                object dbValue = reader.GetValue(ordinal);

                object value = DbValueConverter.ConvertTo(
                    dbValue,
                    column.PropertyInfo.PropertyType);

                if (!column.PropertyInfo.CanWrite)
                    continue;

                column.PropertyInfo.SetValue(
                    entity,
                    value,
                    null);
            }
        }

        /// <summary>
        /// IDataReader의 모든 행을 Entity 목록으로 변환합니다.
        /// </summary>
        /// <typeparam name="T">Entity 타입입니다.</typeparam>
        /// <param name="reader">데이터를 읽을 IDataReader입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        /// <returns>매핑된 Entity 목록입니다.</returns>
        public static List<T> MapList<T>(
            IDataReader reader,
            EntityMetadata metadata)
            where T : new()
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            List<T> result = new List<T>();

            while (reader.Read())
            {
                result.Add(
                    Map<T>(
                        reader,
                        metadata));
            }

            return result;
        }

        #region Column Ordinals

        /// <summary>
        /// IDataReader의 컬럼 이름과 Ordinal을 Dictionary로 생성합니다.
        /// </summary>
        private static Dictionary<string, int> CreateColumnOrdinals(
            IDataReader reader)
        {
            Dictionary<string, int> result =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);

                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                if (!result.ContainsKey(columnName))
                    result.Add(columnName, i);
            }

            return result;
        }

        #endregion

        #region Null

        /// <summary>
        /// DB NULL을 Entity Property에 설정합니다.
        /// </summary>
        private static void SetNullValue(
            object entity,
            PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite)
                return;

            Type propertyType =
                propertyInfo.PropertyType;

            if (DbValueConverter.IsNullable(propertyType))
            {
                propertyInfo.SetValue(
                    entity,
                    null,
                    null);

                return;
            }

            // Value Type은 null을 저장할 수 없으므로
            // 기본값을 사용합니다.
            if (propertyType.IsValueType)
            {
                object defaultValue =
                    Activator.CreateInstance(propertyType);

                propertyInfo.SetValue(
                    entity,
                    defaultValue,
                    null);
            }
            else
            {
                propertyInfo.SetValue(
                    entity,
                    null,
                    null);
            }
        }

        #endregion
    }
}