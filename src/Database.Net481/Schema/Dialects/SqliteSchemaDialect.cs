using System;
using System.Collections.Generic;
using System.Linq;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.Schema.Dialects
{
    /// <summary>
    /// SQLite 데이터베이스의 Schema 및 DDL 생성을 담당합니다.
    /// </summary>
    public sealed class SqliteSchemaDialect : ISchemaDialect
    {
        /// <summary>
        /// SQLite 식별자를 인용합니다.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException(
                    "식별자는 비어 있을 수 없습니다.",
                    nameof(identifier));
            }

            return "\"" +
                   identifier.Replace("\"", "\"\"") +
                   "\"";
        }

        /// <summary>
        /// ColumnMetadata의 .NET 자료형을 SQLite 자료형으로 변환합니다.
        /// </summary>
        public string GetColumnType(ColumnMetadata column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "ColumnMetadata.PropertyInfo가 설정되지 않았습니다.");
            }

            Type propertyType =
                Nullable.GetUnderlyingType(
                    column.PropertyInfo.PropertyType)
                ?? column.PropertyInfo.PropertyType;

            if (propertyType == typeof(string))
                return "TEXT";

            if (propertyType == typeof(char))
                return "TEXT";

            if (propertyType == typeof(bool))
                return "INTEGER";

            if (propertyType == typeof(byte) ||
                propertyType == typeof(short) ||
                propertyType == typeof(int) ||
                propertyType == typeof(long) ||
                propertyType == typeof(sbyte) ||
                propertyType == typeof(ushort) ||
                propertyType == typeof(uint) ||
                propertyType == typeof(ulong))
            {
                return "INTEGER";
            }

            if (propertyType == typeof(float) ||
                propertyType == typeof(double))
            {
                return "REAL";
            }

            if (propertyType == typeof(decimal))
                return "NUMERIC";

            if (propertyType == typeof(DateTime) ||
                propertyType == typeof(DateTimeOffset))
            {
                return "TEXT";
            }

            if (propertyType == typeof(Guid))
                return "TEXT";

            if (propertyType == typeof(byte[]))
                return "BLOB";

            if (propertyType.IsEnum)
                return "INTEGER";

            throw new NotSupportedException(
                string.Format(
                    "SQLite에서 지원하지 않는 Property 타입입니다: {0}",
                    propertyType.FullName));
        }

        /// <summary>
        /// SQLite CREATE TABLE SQL을 생성합니다.
        /// </summary>
        public string BuildCreateTable(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns)
        {
            ValidateTableName(tableName);
            ValidateColumns(columns);

            List<string> definitions =
                new List<string>();

            List<ColumnMetadata> primaryKeys =
                columns
                    .Where(x => x != null && x.IsPrimaryKey)
                    .ToList();

            foreach (ColumnMetadata column in columns)
            {
                string columnDefinition =
                    QuoteIdentifier(column.ColumnName)
                    + " "
                    + GetColumnType(column);

                if (column.IsPrimaryKey &&
                    primaryKeys.Count == 1 &&
                    IsIntegerType(column))
                {
                    columnDefinition +=
                        " PRIMARY KEY AUTOINCREMENT";
                }

                definitions.Add(columnDefinition);
            }

            if (primaryKeys.Count > 1)
            {
                definitions.Add(
                    "PRIMARY KEY (" +
                    string.Join(
                        ", ",
                        primaryKeys.Select(
                            x => QuoteIdentifier(x.ColumnName))) +
                    ")");
            }
            else if (primaryKeys.Count == 1 &&
                     !IsIntegerType(primaryKeys[0]))
            {
                // 단일 비정수 Primary Key는
                // Column 정의에 PRIMARY KEY를 직접 추가합니다.
                int index = definitions.Count - 1;

                ColumnMetadata primaryKey =
                    primaryKeys[0];

                string definition =
                    QuoteIdentifier(primaryKey.ColumnName)
                    + " "
                    + GetColumnType(primaryKey)
                    + " PRIMARY KEY";

                definitions[index] = definition;
            }

            return "CREATE TABLE "
                   + QuoteIdentifier(tableName)
                   + " ("
                   + string.Join(", ", definitions)
                   + ")";
        }

        /// <summary>
        /// SQLite CREATE INDEX SQL을 생성합니다.
        /// </summary>
        public string BuildCreateIndex(
            string tableName,
            IndexMetadata index)
        {
            ValidateTableName(tableName);
            ValidateIndex(index);

            string unique =
                index.IsUnique
                    ? "UNIQUE "
                    : string.Empty;

            string columns =
                string.Join(
                    ", ",
                    index.ColumnNames.Select(
                        QuoteIdentifier));

            return "CREATE "
                   + unique
                   + "INDEX "
                   + QuoteIdentifier(index.IndexName)
                   + " ON "
                   + QuoteIdentifier(tableName)
                   + " ("
                   + columns
                   + ")";
        }

        /// <summary>
        /// SQLite DROP TABLE SQL을 생성합니다.
        /// </summary>
        public string BuildDropTable(
            string tableName)
        {
            ValidateTableName(tableName);

            return "DROP TABLE "
                   + QuoteIdentifier(tableName);
        }

        /// <summary>
        /// SQLite DROP INDEX SQL을 생성합니다.
        /// </summary>
        public string BuildDropIndex(
            string tableName,
            string indexName)
        {
            ValidateTableName(tableName);

            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new ArgumentException(
                    "Index 이름은 비어 있을 수 없습니다.",
                    nameof(indexName));
            }

            return "DROP INDEX "
                   + QuoteIdentifier(indexName);
        }

        private static bool IsIntegerType(
            ColumnMetadata column)
        {
            if (column == null ||
                column.PropertyInfo == null)
            {
                return false;
            }

            Type propertyType =
                Nullable.GetUnderlyingType(
                    column.PropertyInfo.PropertyType)
                ?? column.PropertyInfo.PropertyType;

            return propertyType == typeof(byte) ||
                   propertyType == typeof(short) ||
                   propertyType == typeof(int) ||
                   propertyType == typeof(long) ||
                   propertyType == typeof(sbyte) ||
                   propertyType == typeof(ushort) ||
                   propertyType == typeof(uint) ||
                   propertyType == typeof(ulong);
        }

        private static void ValidateTableName(
            string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "Table 이름은 비어 있을 수 없습니다.",
                    nameof(tableName));
            }
        }

        private static void ValidateColumns(
            IReadOnlyList<ColumnMetadata> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(
                    nameof(columns));
            }

            if (columns.Count == 0)
            {
                throw new ArgumentException(
                    "Table에는 하나 이상의 Column이 필요합니다.",
                    nameof(columns));
            }

            foreach (ColumnMetadata column in columns)
            {
                if (column == null)
                {
                    throw new ArgumentException(
                        "Column 목록에 null 항목이 포함되어 있습니다.",
                        nameof(columns));
                }

                if (string.IsNullOrWhiteSpace(
                        column.ColumnName))
                {
                    throw new InvalidOperationException(
                        "ColumnName이 비어 있습니다.");
                }

                if (column.PropertyInfo == null)
                {
                    throw new InvalidOperationException(
                        "ColumnMetadata.PropertyInfo가 설정되지 않았습니다.");
                }
            }
        }

        private static void ValidateIndex(
            IndexMetadata index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(
                    nameof(index));
            }

            if (string.IsNullOrWhiteSpace(
                    index.IndexName))
            {
                throw new InvalidOperationException(
                    "IndexName이 비어 있습니다.");
            }

            if (index.ColumnNames == null ||
                index.ColumnNames.Length == 0)
            {
                throw new InvalidOperationException(
                    "Index를 구성하는 Column이 없습니다.");
            }

            foreach (string columnName in index.ColumnNames)
            {
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    throw new InvalidOperationException(
                        "Index Column 이름이 비어 있습니다.");
                }
            }
        }
    }
}