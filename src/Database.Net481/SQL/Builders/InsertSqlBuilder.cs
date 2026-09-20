using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Builders
{
    /// <summary>
    /// Entity Metadata를 기반으로 INSERT SQL을 생성합니다.
    /// 실제 DB별 SQL 문법은 ISqlDialect에 위임합니다.
    /// </summary>
    public static class InsertSqlBuilder
    {
        /// <summary>
        /// Entity Metadata를 기반으로 INSERT SQL을 생성합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <returns>
        /// 생성된 INSERT SQL입니다.
        /// </returns>
        public static string Build(
            EntityMetadata metadata,
            ISqlDialect dialect)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (dialect == null)
            {
                throw new ArgumentNullException(nameof(dialect));
            }

            if (string.IsNullOrWhiteSpace(metadata.TableName))
            {
                throw new InvalidOperationException(
                    "Entity Metadata의 TableName이 비어 있습니다.");
            }

            List<ColumnMetadata> columns =
                GetInsertableColumns(metadata);

            if (columns.Count == 0)
            {
                throw new InvalidOperationException(
                    "INSERT 가능한 Column이 없습니다.");
            }

            return dialect.BuildInsert(
                metadata.TableName,
                columns);
        }

        /// <summary>
        /// INSERT에 사용할 컬럼을 가져옵니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <returns>
        /// INSERT 가능한 Column 목록입니다.
        /// </returns>
        private static List<ColumnMetadata> GetInsertableColumns(
            EntityMetadata metadata)
        {
            List<ColumnMetadata> result =
                new List<ColumnMetadata>();

            foreach (ColumnMetadata column in metadata.Columns)
            {
                if (column == null)
                {
                    continue;
                }

                if (!column.IsInsertable)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(column.ColumnName))
                {
                    throw new InvalidOperationException(
                        "Insert 대상 Column의 ColumnName이 비어 있습니다.");
                }

                if (column.PropertyInfo == null)
                {
                    throw new InvalidOperationException(
                        "Insert 대상 Column의 PropertyInfo가 설정되지 않았습니다.");
                }

                result.Add(column);
            }

            return result;
        }
    }
}

/*
이 Builder의 역할

중요한 것은 InsertSqlBuilder가 DB별 SQL을 알지 않는다는 점입니다.

예를 들어 같은 Entity라도:

InsertSqlBuilder
       │
       ├── SQLiteDialect
       │       ↓
       │   SQLite INSERT
       │
       ├── MariaDbDialect
       │       ↓
       │   MariaDB INSERT
       │
       └── PostgreSqlDialect
               ↓
           PostgreSQL INSERT

구조입니다.

실제로 Builder가 하는 일은:

EntityMetadata.Columns
        ↓
IsInsertable = true
        ↓
INSERT 대상 컬럼 목록
        ↓
ISqlDialect.BuildInsert()

입니다.

그리고 이 부분이 나중에 ParameterMapper와 정확히 대응됩니다.

InsertSqlBuilder
    ↓
INSERT 컬럼 결정
    ↓
Column A
Column B
Column C

ParameterMapper
    ↓
같은 컬럼의 값 결정
    ↓
@A
@B
@C

따라서 SQL과 Parameter의 컬럼 순서/대상이 일치하게 됩

*/
