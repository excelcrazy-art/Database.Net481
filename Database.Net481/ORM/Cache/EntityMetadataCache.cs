using System;
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

        private static readonly object _lock = new object();

        /// <summary>
        /// 지정한 엔티티 타입의 메타데이터를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <returns>엔티티 메타데이터</returns>
        public static EntityMetadata GetMetadata<T>()
        {
            return GetMetadata(typeof(T));
        }

        /// <summary>
        /// 지정한 타입의 메타데이터를 가져옵니다.
        /// </summary>
        /// <param name="type">엔티티 타입</param>
        /// <returns>엔티티 메타데이터</returns>
        public static EntityMetadata GetMetadata(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            lock (_lock)
            {
                EntityMetadata cachedMetadata;

                if (_cache.TryGetValue(type, out cachedMetadata))
                {
                    return cachedMetadata;
                }

                var metadata = new EntityMetadata();

                var tableAttribute =
                    type.GetCustomAttribute<TableAttribute>();

                if (tableAttribute == null)
                {
                    throw new InvalidOperationException(
                        $"{type.Name}에 TableAttribute가 없습니다.");
                }

                metadata.TableName = tableAttribute.Name;
                metadata.Columns = new List<ColumnMetadata>();

                foreach (PropertyInfo property in type.GetProperties())
                {
                    var columnAttribute =
                        property.GetCustomAttribute<ColumnAttribute>();

                    if (columnAttribute == null)
                        continue;

                    metadata.Columns.Add(
                        new ColumnMetadata
                        {
                            PropertyInfo = property,
                            ColumnName = columnAttribute.Name,
                            IsPrimaryKey = columnAttribute.IsPrimaryKey,
                            IsInsertable = columnAttribute.IsInsertable,
                            IsUpdatable = columnAttribute.IsUpdatable,
                            StoreDateOnly = columnAttribute.StoreDateOnly
                        });
                }

                if (metadata.Columns.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{type.Name}에 ColumnAttribute가 정의된 속성이 없습니다.");
                }

                _cache[type] = metadata;

                return metadata;
            }
        }
    }
}