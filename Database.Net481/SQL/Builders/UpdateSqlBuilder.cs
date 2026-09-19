using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Builders
{
    /// <summary>
    /// Entity Metadata를 기반으로 UPDATE SQL을 생성합니다.
    /// 실제 DB별 SQL 문법은 ISqlDialect에 위임합니다.
    /// </summary>
    public static class UpdateSqlBuilder
    {
        /// <summary>
        /// Entity Metadata를 기반으로 UPDATE SQL을 생성합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <param name="dialect">
        /// 사용할 SQL Dialect입니다.
        /// </param>
        /// <returns>
        /// 생성된 UPDATE SQL입니다.
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

            List<ColumnMetadata> updateColumns =
                GetUpdateColumns(metadata);

            List<ColumnMetadata> primaryKeys =
                GetPrimaryKeys(metadata);

            if (updateColumns.Count == 0)
            {
                throw new InvalidOperationException(
                    "UPDATE 가능한 Column이 없습니다.");
            }

            if (primaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "UPDATE에 사용할 Primary Key가 없습니다.");
            }

            return dialect.BuildUpdate(
                metadata.TableName,
                updateColumns,
                primaryKeys);
        }

        /// <summary>
        /// UPDATE 대상 컬럼을 가져옵니다.
        /// Primary Key는 UPDATE 대상에서 제외합니다.
        /// </summary>
        /// <param name="metadata">
        /// Entity Metadata입니다.
        /// </param>
        /// <returns>
        /// UPDATE 가능한 Column 목록입니다.
        /// </returns>
        private static List<ColumnMetadata> GetUpdateColumns(
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

                if (column.IsPrimaryKey)
                {
                    continue;
                }

                if (!column.IsUpdatable)
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
                    "Column의 ColumnName이 비어 있습니다.");
            }

            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "Column의 PropertyInfo가 설정되지 않았습니다.");
            }
        }
    }
}

/*

핵심은 수정 대상 컬럼과 Primary Key를 분리하는 것입니다.

EntityMetadata
     │
     ├── IsUpdatable = true
     │       ↓
     │    SET 컬럼
     │
     └── IsPrimaryKey = true
             ↓
          WHERE 조건

그리고 실제 SQL 문법은 ISqlDialect.BuildUpdate()에 맡깁니다.


생성되는 SQL의 구조

예를 들어 Entity가:

Id          PK
Name        Updatable
Price       Updatable
CreatedDate Not Updatable

라면 UpdateSqlBuilder는:

UPDATE 대상
    Name
    Price

WHERE 대상
    Id

를 Dialect에 전달합니다.

SQLite라면 최종적으로 대략:

UPDATE "Stock"
SET "Name" = @Name,
    "Price" = @Price
WHERE "Id" = @Id

MariaDB:

UPDATE `Stock`
SET `Name` = @Name,
    `Price` = @Price
WHERE `Id` = @Id

PostgreSQL:

UPDATE "Stock"
SET "Name" = @Name,
    "Price" = @Price
WHERE "Id" = @Id

처럼 됩니다.

그리고 이것이 현재 ParameterMapper와 정확히 대응합니다.

UpdateSqlBuilder
    │
    ├── UPDATE columns
    │      ↓
    │   IsUpdatable
    │
    └── Primary Keys
           ↓
        WHERE

ParameterMapper
    │
    ├── AddUpdateParameters()
    │      ↓
    │   UPDATE 값
    │
    └── AddPrimaryKeyParameters()
           ↓
        WHERE 값

특히 Primary Key가 없는 Entity에 대해서는 UPDATE SQL을 생성하지 않도록 막았습니다. 이것은 실수로 전체 테이블을 업데이트하는 것을 방지하는 데 중요

*/
