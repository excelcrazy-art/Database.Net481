using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Builders
{
    /// <summary>
    /// Entity Metadata를 기반으로 Upsert SQL을 생성합니다.
    /// 실제 DB별 Upsert 문법은 ISqlDialect에 위임합니다.
    /// </summary>
    public static class UpsertSqlBuilder
    {
        /// <summary>
        /// Upsert SQL을 생성합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <returns>
        /// 생성된 Upsert SQL입니다.
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

            List<ColumnMetadata> insertColumns =
                GetInsertColumns(metadata);

            List<ColumnMetadata> primaryKeys =
                GetPrimaryKeys(metadata);

            if (insertColumns.Count == 0)
            {
                throw new InvalidOperationException(
                    "UPSERT 가능한 Insert Column이 없습니다.");
            }

            if (primaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "UPSERT에 사용할 Primary Key가 없습니다.");
            }

            return dialect.BuildUpsert(
                metadata.TableName,
                insertColumns,
                primaryKeys);
        }

        /// <summary>
        /// Upsert 시 INSERT에 사용할 컬럼을 가져옵니다.
        /// </summary>
        private static List<ColumnMetadata> GetInsertColumns(
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

                ValidateColumn(column);

                result.Add(column);
            }

            return result;
        }

        /// <summary>
        /// Primary Key 컬럼을 가져옵니다.
        /// </summary>
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
        /// Column Metadata의 필수 정보를 검증합니다.
        /// </summary>
        private static void ValidateColumn(
            ColumnMetadata column)
        {
            if (string.IsNullOrWhiteSpace(column.ColumnName))
            {
                throw new InvalidOperationException(
                    "Upsert 대상 Column의 ColumnName이 비어 있습니다.");
            }

            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "Upsert 대상 Column의 PropertyInfo가 설정되지 않았습니다.");
            }
        }
    }
}

/*
UpsertSqlBuilder가 INSERT 대상 컬럼과 Primary Key를 Metadata에서 추출한 뒤 ISqlDialect.BuildUpsert()에 위임하면 됨


이 코드는 UpsertSqlBuilder 자체에서 SQLite, MariaDB, PostgreSQL의 문법을 전혀 알지 않습니다.

예를 들어 같은 Metadata에 대해:

SQLite

INSERT INTO "Stock" (...)
VALUES (...)
ON CONFLICT ("StockCode")
DO UPDATE SET ...

MariaDB

INSERT INTO `Stock` (...)
VALUES (...)
ON DUPLICATE KEY UPDATE ...

PostgreSQL

INSERT INTO "Stock" (...)
VALUES (...)
ON CONFLICT ("StockCode")
DO UPDATE SET ...

이 차이는 이미 작성한 각각의 ISqlDialect 구현이 담당합니다.

따라서 구조가 깔끔하게:

EntityMetadata
      │
      ▼
UpsertSqlBuilder
      │
      ▼
ISqlDialect
      │
 ┌────┼────────┐
 ▼    ▼        ▼
SQLite MariaDB PostgreSQL

로 유지됩니다.

그리고 현재 ParameterMapper에는 AddUpsertParameters()가 따로 없지만 지금 당장 추가할 필요는 없습니다. 
Upsert SQL이 만들어지는 것과 실제 파라미터를 연결하는 부분은 Repository CRUD를 구현하면서 같이 맞추는 편이 좋습니다.

*/
