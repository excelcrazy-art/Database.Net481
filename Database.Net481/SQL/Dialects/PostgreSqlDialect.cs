using System;
using System.Collections.Generic;
using System.Text;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL.Dialects
{
    /// <summary>
    /// PostgreSQL 데이터베이스의 SQL 문법을 구현합니다.
    /// </summary>
    public sealed class PostgreSqlDialect : ISqlDialect
    {
        #region Identifier / Parameter

        /// <summary>
        /// PostgreSQL 식별자를 큰따옴표로 감쌉니다.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException(
                    "식별자는 비어 있을 수 없습니다.",
                    nameof(identifier));

            // PostgreSQL에서는 " 문자를 ""로 escape합니다.
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// PostgreSQL용 파라미터 이름을 반환합니다.
        /// </summary>
        public string GetParameterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "파라미터 이름은 비어 있을 수 없습니다.",
                    nameof(name));

            return "@" + name;
        }

        #endregion

        #region Insert

        /// <summary>
        /// PostgreSQL INSERT SQL을 생성합니다.
        /// </summary>
        public string BuildInsert(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns)
        {
            ValidateTableName(tableName);
            ValidateColumns(columns);

            StringBuilder columnBuilder = new StringBuilder();
            StringBuilder parameterBuilder = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                {
                    columnBuilder.Append(", ");
                    parameterBuilder.Append(", ");
                }

                columnBuilder.Append(
                    QuoteIdentifier(columns[i].ColumnName));

                parameterBuilder.Append(
                    GetParameterName(columns[i].ColumnName));
            }

            return string.Format(
                "INSERT INTO {0} ({1}) VALUES ({2});",
                QuoteIdentifier(tableName),
                columnBuilder,
                parameterBuilder);
        }

        #endregion

        #region Update

        /// <summary>
        /// PostgreSQL UPDATE SQL을 생성합니다.
        /// </summary>
        public string BuildUpdate(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            IReadOnlyList<ColumnMetadata> primaryKeys)
        {
            ValidateTableName(tableName);
            ValidateColumns(columns);
            ValidatePrimaryKeys(primaryKeys);

            StringBuilder setBuilder = new StringBuilder();
            StringBuilder whereBuilder = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    setBuilder.Append(", ");

                setBuilder.Append(
                    QuoteIdentifier(columns[i].ColumnName));

                setBuilder.Append(" = ");

                setBuilder.Append(
                    GetParameterName(columns[i].ColumnName));
            }

            for (int i = 0; i < primaryKeys.Count; i++)
            {
                if (i > 0)
                    whereBuilder.Append(" AND ");

                whereBuilder.Append(
                    QuoteIdentifier(primaryKeys[i].ColumnName));

                whereBuilder.Append(" = ");

                whereBuilder.Append(
                    GetParameterName(primaryKeys[i].ColumnName));
            }

            return string.Format(
                "UPDATE {0} SET {1} WHERE {2};",
                QuoteIdentifier(tableName),
                setBuilder,
                whereBuilder);
        }

        #endregion

        #region Delete

        /// <summary>
        /// PostgreSQL DELETE SQL을 생성합니다.
        /// </summary>
        public string BuildDelete(
            string tableName,
            IReadOnlyList<ColumnMetadata> primaryKeys)
        {
            ValidateTableName(tableName);
            ValidatePrimaryKeys(primaryKeys);

            StringBuilder whereBuilder = new StringBuilder();

            for (int i = 0; i < primaryKeys.Count; i++)
            {
                if (i > 0)
                    whereBuilder.Append(" AND ");

                whereBuilder.Append(
                    QuoteIdentifier(primaryKeys[i].ColumnName));

                whereBuilder.Append(" = ");

                whereBuilder.Append(
                    GetParameterName(primaryKeys[i].ColumnName));
            }

            return string.Format(
                "DELETE FROM {0} WHERE {1};",
                QuoteIdentifier(tableName),
                whereBuilder);
        }

        #endregion

        #region Select

        /// <summary>
        /// PostgreSQL SELECT SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="columns">조회할 컬럼 목록</param>
        /// <param name="whereClause">
        /// WHERE 조건식입니다.
        /// 예: "StockCode = @StockCode"
        /// </param>
        public string BuildSelect(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            string whereClause = null)
        {
            ValidateTableName(tableName);
            ValidateColumns(columns);

            StringBuilder columnBuilder = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    columnBuilder.Append(", ");

                columnBuilder.Append(
                    QuoteIdentifier(columns[i].ColumnName));
            }

            StringBuilder sql = new StringBuilder();

            sql.Append("SELECT ");
            sql.Append(columnBuilder);
            sql.Append(" FROM ");
            sql.Append(QuoteIdentifier(tableName));

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql.Append(" WHERE ");
                sql.Append(whereClause);
            }

            sql.Append(";");

            return sql.ToString();
        }

        #endregion

        #region Upsert

        /// <summary>
        /// PostgreSQL UPSERT SQL을 생성합니다.
        ///
        /// PostgreSQL:
        /// INSERT ... ON CONFLICT (...) DO UPDATE SET ...
        /// </summary>
        public string BuildUpsert(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            IReadOnlyList<ColumnMetadata> primaryKeys)
        {
            ValidateTableName(tableName);
            ValidateColumns(columns);
            ValidatePrimaryKeys(primaryKeys);

            StringBuilder columnBuilder = new StringBuilder();
            StringBuilder parameterBuilder = new StringBuilder();

            // INSERT 컬럼 및 파라미터
            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                {
                    columnBuilder.Append(", ");
                    parameterBuilder.Append(", ");
                }

                columnBuilder.Append(
                    QuoteIdentifier(columns[i].ColumnName));

                parameterBuilder.Append(
                    GetParameterName(columns[i].ColumnName));
            }

            // ON CONFLICT 대상 Primary Key
            StringBuilder conflictBuilder =
                new StringBuilder();

            for (int i = 0; i < primaryKeys.Count; i++)
            {
                if (i > 0)
                    conflictBuilder.Append(", ");

                conflictBuilder.Append(
                    QuoteIdentifier(primaryKeys[i].ColumnName));
            }

            // PK가 아닌 컬럼만 UPDATE
            List<ColumnMetadata> updateColumns =
                new List<ColumnMetadata>();

            for (int i = 0; i < columns.Count; i++)
            {
                if (!IsPrimaryKey(
                    columns[i],
                    primaryKeys))
                {
                    updateColumns.Add(columns[i]);
                }
            }

            StringBuilder sql = new StringBuilder();

            sql.Append("INSERT INTO ");
            sql.Append(QuoteIdentifier(tableName));
            sql.Append(" (");
            sql.Append(columnBuilder);
            sql.Append(") VALUES (");
            sql.Append(parameterBuilder);
            sql.Append(") ON CONFLICT (");
            sql.Append(conflictBuilder);
            sql.Append(") ");

            if (updateColumns.Count == 0)
            {
                // PK만 있는 경우 충돌 시 아무 작업도 하지 않습니다.
                sql.Append("DO NOTHING;");
            }
            else
            {
                sql.Append("DO UPDATE SET ");

                for (int i = 0; i < updateColumns.Count; i++)
                {
                    if (i > 0)
                        sql.Append(", ");

                    string columnName =
                        QuoteIdentifier(
                            updateColumns[i].ColumnName);

                    sql.Append(columnName);
                    sql.Append(" = EXCLUDED.");
                    sql.Append(columnName);
                }

                sql.Append(";");
            }

            return sql.ToString();
        }

        #endregion

        #region Validation

        private static void ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "테이블 이름은 비어 있을 수 없습니다.",
                    nameof(tableName));
            }
        }

        private static void ValidateColumns(
            IReadOnlyList<ColumnMetadata> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentException(
                    "컬럼 목록은 비어 있을 수 없습니다.",
                    nameof(columns));
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i] == null)
                {
                    throw new ArgumentException(
                        "컬럼 목록에 null이 포함되어 있습니다.",
                        nameof(columns));
                }

                if (string.IsNullOrWhiteSpace(
                    columns[i].ColumnName))
                {
                    throw new ArgumentException(
                        "컬럼 이름은 비어 있을 수 없습니다.",
                        nameof(columns));
                }
            }
        }

        private static void ValidatePrimaryKeys(
            IReadOnlyList<ColumnMetadata> primaryKeys)
        {
            if (primaryKeys == null || primaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "UPDATE, DELETE, UPSERT에는 하나 이상의 Primary Key가 필요합니다.");
            }

            for (int i = 0; i < primaryKeys.Count; i++)
            {
                if (primaryKeys[i] == null)
                {
                    throw new ArgumentException(
                        "Primary Key 목록에 null이 포함되어 있습니다.",
                        nameof(primaryKeys));
                }

                if (string.IsNullOrWhiteSpace(
                    primaryKeys[i].ColumnName))
                {
                    throw new ArgumentException(
                        "Primary Key 컬럼 이름은 비어 있을 수 없습니다.",
                        nameof(primaryKeys));
                }
            }
        }

        private static bool IsPrimaryKey(
            ColumnMetadata column,
            IReadOnlyList<ColumnMetadata> primaryKeys)
        {
            for (int i = 0; i < primaryKeys.Count; i++)
            {
                if (string.Equals(
                    column.ColumnName,
                    primaryKeys[i].ColumnName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}