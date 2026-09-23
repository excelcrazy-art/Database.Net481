﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Database.Net481.Attributes;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.ORM.Cache
{
    /// <summary>
    /// 엔티티 메타데이터를 캐시합니다.
    /// </summary>
    public static class EntityMetadataCache
    {
        private static readonly Dictionary<Type, EntityMetadata> _cache =
            new Dictionary<Type, EntityMetadata>();

        private static readonly object _lock =
            new object();

        /// <summary>
        /// 지정한 엔티티 타입의 메타데이터를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// 엔티티 타입입니다.
        /// </typeparam>
        /// <returns>
        /// 엔티티 메타데이터입니다.
        /// </returns>
        public static EntityMetadata GetMetadata<T>()
        {
            return GetMetadata(typeof(T));
        }

        /// <summary>
        /// 지정한 타입의 메타데이터를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// 엔티티 타입입니다.
        /// </param>
        /// <returns>
        /// 엔티티 메타데이터입니다.
        /// </returns>
        public static EntityMetadata GetMetadata(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            lock (_lock)
            {
                EntityMetadata cachedMetadata;

                if (_cache.TryGetValue(
                    type,
                    out cachedMetadata))
                {
                    return cachedMetadata;
                }

                EntityMetadata metadata =
                    BuildMetadata(type);

                _cache[type] = metadata;

                return metadata;
            }
        }

        /// <summary>
        /// 지정한 타입의 Entity Metadata를 생성합니다.
        /// </summary>
        private static EntityMetadata BuildMetadata(
            Type type)
        {
            TableAttribute tableAttribute =
                type.GetCustomAttribute<TableAttribute>();

            if (tableAttribute == null)
            {
                throw new InvalidOperationException(
                    type.Name +
                    "에 TableAttribute가 없습니다.");
            }

            EntityMetadata metadata =
                new EntityMetadata();

            metadata.TableName =
                tableAttribute.Name;

            metadata.Columns =
                new List<ColumnMetadata>();

            metadata.Indexes =
                new List<IndexMetadata>();

            BuildColumns(
                type,
                metadata);

            BuildIndexes(
                type,
                metadata);

            return metadata;
        }

        /// <summary>
        /// Entity의 Column Metadata를 생성합니다.
        /// </summary>
        private static void BuildColumns(
            Type type,
            EntityMetadata metadata)
        {
            foreach (PropertyInfo property
                in type.GetProperties())
            {
                ColumnAttribute columnAttribute =
                    property.GetCustomAttribute<ColumnAttribute>();

                if (columnAttribute == null)
                    continue;

                metadata.Columns.Add(
                    new ColumnMetadata
                    {
                        PropertyInfo = property,
                        ColumnName = columnAttribute.Name,
                        IsPrimaryKey =
                            columnAttribute.IsPrimaryKey,
                        IsInsertable =
                            columnAttribute.IsInsertable,
                        IsUpdatable =
                            columnAttribute.IsUpdatable,
                        StoreDateOnly =
                            columnAttribute.StoreDateOnly
                    });
            }

            if (metadata.Columns.Count == 0)
            {
                throw new InvalidOperationException(
                    type.Name +
                    "에 ColumnAttribute가 정의된 속성이 없습니다.");
            }
        }

        /// <summary>
        /// Entity의 Index Metadata를 생성합니다.
        /// </summary>
        private static void BuildIndexes(
            Type type,
            EntityMetadata metadata)
        {
            IndexAttribute[] indexAttributes =
                (IndexAttribute[])type.GetCustomAttributes(
                    typeof(IndexAttribute),
                    true);

            foreach (IndexAttribute attribute
                in indexAttributes)
            {
                if (attribute == null)
                    continue;

                ValidateIndex(
                    type,
                    metadata,
                    attribute);

                metadata.Indexes.Add(
                    new IndexMetadata
                    {
                        IndexName = attribute.Name,
                        ColumnNames =
                            (string[])attribute.Columns.Clone(),
                        IsUnique = attribute.IsUnique
                    });
            }
        }

        /// <summary>
        /// IndexAttribute의 내용을 검증합니다.
        /// </summary>
        private static void ValidateIndex(
            Type type,
            EntityMetadata metadata,
            IndexAttribute attribute)
        {
            if (string.IsNullOrWhiteSpace(
                attribute.Name))
            {
                throw new InvalidOperationException(
                    type.Name +
                    "의 Index 이름이 비어 있습니다.");
            }

            if (attribute.Columns == null ||
                attribute.Columns.Length == 0)
            {
                throw new InvalidOperationException(
                    type.Name +
                    "의 Index에 컬럼이 정의되지 않았습니다.");
            }

            HashSet<string> columnNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (ColumnMetadata column
                in metadata.Columns)
            {
                if (column == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                    column.ColumnName))
                {
                    continue;
                }

                columnNames.Add(
                    column.ColumnName);
            }

            foreach (string columnName
                in attribute.Columns)
            {
                if (string.IsNullOrWhiteSpace(
                    columnName))
                {
                    throw new InvalidOperationException(
                        type.Name +
                        "의 Index '" +
                        attribute.Name +
                        "'에 비어 있는 컬럼 이름이 있습니다.");
                }

                if (!columnNames.Contains(
                    columnName))
                {
                    throw new InvalidOperationException(
                        type.Name +
                        "의 Index '" +
                        attribute.Name +
                        "'가 존재하지 않는 Column '" +
                        columnName +
                        "'을 참조합니다.");
                }
            }
        }
    }
}
