using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Builders
{
    /// <summary>
    /// Entity Metadata를 기반으로 DELETE SQL을 생성합니다.
    /// 실제 DB별 SQL 문법은 ISqlDialect에 위임합니다.
    /// </summary>
    public static class DeleteSqlBuilder
    {
        /// <summary>
        /// Entity Metadata를 기반으로 DELETE SQL을 생성합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <returns>
        /// 생성된 DELETE SQL입니다.
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

            List<ColumnMetadata> primaryKeys =
                GetPrimaryKeys(metadata);

            if (primaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "DELETE에 사용할 Primary Key가 없습니다.");
            }

            return dialect.BuildDelete(
                metadata.TableName,
                primaryKeys);
        }

        /// <summary>
        /// Entity Metadata에서 Primary Key 컬럼을 가져옵니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <returns>
        /// Primary Key Column 목록입니다.
        /// </returns>
        private static List<ColumnMetadata> GetPrimaryKeys(
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

                if (!column.IsPrimaryKey)
                {
                    continue;
                }

                ValidateColumn(column);

                result.Add(column);
            }

            return result;
        }

        /// <summary>
        /// Primary Key Column Metadata의 필수 정보를 검증합니다.
        /// </summary>
        /// <param name="column">
        /// 검증할 Column Metadata입니다.
        /// </param>
        private static void ValidateColumn(
            ColumnMetadata column)
        {
            if (string.IsNullOrWhiteSpace(column.ColumnName))
            {
                throw new InvalidOperationException(
                    "Primary Key Column의 ColumnName이 비어 있습니다.");
            }

            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "Primary Key Column의 PropertyInfo가 설정되지 않았습니다.");
            }
        }
    }
}

/*

핵심은 Primary Key만 WHERE 조건으로 사용하는 것입니다. 그래야 Delete(entity)가 실수로 테이블 전체를 삭제하는 상황을 구조적으로 막을 수 있습니다.

구조

DeleteSqlBuilder는 아주 명확합니다.

EntityMetadata
      │
      ▼
Primary Key 검색
      │
      ▼
Primary Key가 있는가?
      │
 ┌────┴────┐
 │         │
없음       있음
 │         │
예외       ▼
       ISqlDialect
           │
           ▼
       DELETE SQL

예를 들어:

Stock
 ├─ StockCode  ← PK
 ├─ Name
 ├─ Price
 └─ Volume

이라면:

DELETE FROM "Stock"
WHERE "StockCode" = @StockCode

가 됩니다.

그리고 ParameterMapper에서는 이미:

ParameterMapper.AddPrimaryKeyParameters(
    command,
    entity,
    metadata);

가 있으므로 다음과 같이 정확히 연결됩니다.

DeleteSqlBuilder
      │
      └── WHERE StockCode = @StockCode
                         ▲
                         │
ParameterMapper ─────────┘
      │
      └── Entity.StockCode
특히 중요한 안전장치

Primary Key가 없는 Entity에서:

repository.Delete(entity);

를 호출했을 때

DELETE FROM Table

같은 SQL이 만들어지는 것을 허용하지 않습니다.

즉:

if (primaryKeys.Count == 0)
{
    throw ...
}

가 의도적으로 들어가 있습니다.

*/
