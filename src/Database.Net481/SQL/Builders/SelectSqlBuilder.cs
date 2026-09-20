using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Builders
{
    /// <summary>
    /// Entity Metadata를 기반으로 SELECT SQL을 생성합니다.
    /// 실제 DB별 SQL 문법은 ISqlDialect에 위임합니다.
    /// </summary>
    public static class SelectSqlBuilder
    {
        /// <summary>
        /// Entity의 모든 컬럼을 조회하는 SELECT SQL을 생성합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <returns>
        /// 생성된 SELECT SQL입니다.
        /// </returns>
        public static string Build(
            EntityMetadata metadata,
            ISqlDialect dialect)
        {
            return Build(
                metadata,
                dialect,
                null);
        }

        /// <summary>
        /// Entity의 모든 컬럼을 조회하는 SELECT SQL을 생성합니다.
        /// WHERE 조건을 지정할 수 있습니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <param name="whereClause">
        /// WHERE 절에 사용할 조건입니다.
        /// 예: "Id = @Id"
        /// </param>
        /// <returns>
        /// 생성된 SELECT SQL입니다.
        /// </returns>
        public static string Build(
            EntityMetadata metadata,
            ISqlDialect dialect,
            string whereClause)
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
                GetSelectColumns(metadata);

            if (columns.Count == 0)
            {
                throw new InvalidOperationException(
                    "SELECT 가능한 Column이 없습니다.");
            }

            return dialect.BuildSelect(
                metadata.TableName,
                columns,
                NormalizeWhereClause(whereClause));
        }

        /// <summary>
        /// SELECT 대상 컬럼을 가져옵니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <returns>
        /// SELECT 대상 Column 목록입니다.
        /// </returns>
        private static List<ColumnMetadata> GetSelectColumns(
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

                ValidateColumn(column);

                result.Add(column);
            }

            return result;
        }

        /// <summary>
        /// WHERE 조건을 정규화합니다.
        /// </summary>
        /// <param name="whereClause">
        /// WHERE 조건입니다.
        /// </param>
        /// <returns>
        /// 정규화된 WHERE 조건입니다.
        /// </returns>
        private static string NormalizeWhereClause(
            string whereClause)
        {
            if (string.IsNullOrWhiteSpace(whereClause))
            {
                return null;
            }

            string result = whereClause.Trim();

            if (result.StartsWith(
                    "WHERE ",
                    StringComparison.OrdinalIgnoreCase))
            {
                return result.Substring(6).Trim();
            }

            if (string.Equals(
                    result,
                    "WHERE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Column Metadata의 필수 정보를 검증합니다.
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
                    "SELECT 대상 Column의 ColumnName이 비어 있습니다.");
            }

            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "SELECT 대상 Column의 PropertyInfo가 설정되지 않았습니다.");
            }
        }
    }
}
/*

SelectSqlBuilder는 앞의 Builder들과 조금 다릅니다.

현재 ISqlDialect의 정의가:

string BuildSelect(
    string tableName,
    IReadOnlyList<ColumnMetadata> columns,
    string whereClause = null);

이므로 SelectSqlBuilder는 SELECT 대상 컬럼을 Metadata에서 가져오고, 선택적으로 WHERE 조건을 전달하는 역할을 하면 됩니다.

    
    
이 Builder의 역할

예를 들어 Entity Metadata가:

Stock
├─ StockCode
├─ Name
├─ Price
└─ Volume

이면:

SelectSqlBuilder.Build(
    metadata,
    dialect);

를 통해 대략 다음 SQL이 만들어집니다.

SELECT "StockCode",
       "Name",
       "Price",
       "Volume"
FROM "Stock"

WHERE 조건을 주면:

SelectSqlBuilder.Build(
    metadata,
    dialect,
    "StockCode = @StockCode");

결과는:

SELECT "StockCode",
       "Name",
       "Price",
       "Volume"
FROM "Stock"
WHERE "StockCode" = @StockCode

가 됩니다.

WHERE를 정규화한 이유

호출자가:

"StockCode = @StockCode"

라고 넣어도 되고,

"WHERE StockCode = @StockCode"

라고 넣어도 되도록 했습니다.

내부적으로는 ISqlDialect.BuildSelect()에 WHERE 조건 자체만 전달합니다.

즉:

SelectSqlBuilder
       │
       ├─ TableName
       ├─ Columns
       └─ Where 조건
              │
              ▼
        ISqlDialect
              │
       ┌──────┼──────┐
       ▼      ▼      ▼
    SQLite MariaDB PostgreSQL

구조입니다.
 */
