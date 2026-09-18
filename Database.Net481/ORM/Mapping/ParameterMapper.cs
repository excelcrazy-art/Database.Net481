using System;
using System.Collections.Generic;
using System.Data;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.ORM.Mapping
{
    /// <summary>
    /// Entity의 Property 값을 데이터베이스 Parameter로 변환합니다.
    /// </summary>
    public static class ParameterMapper
    {
        #region Insert

        /// <summary>
        /// Entity의 Insert 가능한 컬럼을 DB Parameter로 추가합니다.
        /// </summary>
        /// <param name="command">DB Command입니다.</param>
        /// <param name="entity">Entity 객체입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        public static void AddInsertParameters(
            IDbCommand command,
            object entity,
            EntityMetadata metadata)
        {
            AddParameters(
                command,
                entity,
                metadata,
                ParameterMode.Insert);
        }

        #endregion

        #region Update

        /// <summary>
        /// Entity의 Update 가능한 컬럼과 Primary Key를
        /// DB Parameter로 추가합니다.
        /// </summary>
        /// <param name="command">DB Command입니다.</param>
        /// <param name="entity">Entity 객체입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        public static void AddUpdateParameters(
            IDbCommand command,
            object entity,
            EntityMetadata metadata)
        {
            AddParameters(
                command,
                entity,
                metadata,
                ParameterMode.Update);
        }

        #endregion

        #region Primary Key

        /// <summary>
        /// Entity의 Primary Key를 DB Parameter로 추가합니다.
        /// </summary>
        /// <param name="command">DB Command입니다.</param>
        /// <param name="entity">Entity 객체입니다.</param>
        /// <param name="metadata">Entity Metadata입니다.</param>
        public static void AddPrimaryKeyParameters(
            IDbCommand command,
            object entity,
            EntityMetadata metadata)
        {
            ValidateArguments(command, entity, metadata);

            foreach (ColumnMetadata column in metadata.PrimaryKeys)
            {
                AddParameter(
                    command,
                    column,
                    entity);
            }
        }

        #endregion

        #region All Parameters

        /// <summary>
        /// 지정한 컬럼들을 DB Parameter로 추가합니다.
        /// </summary>
        /// <param name="command">DB Command입니다.</param>
        /// <param name="entity">Entity 객체입니다.</param>
        /// <param name="columns">Parameter로 추가할 컬럼입니다.</param>
        public static void AddParameters(
            IDbCommand command,
            object entity,
            IEnumerable<ColumnMetadata> columns)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            foreach (ColumnMetadata column in columns)
            {
                if (column == null)
                    continue;

                AddParameter(
                    command,
                    column,
                    entity);
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// Parameter Mode에 따라 Entity의 컬럼을 Parameter로 추가합니다.
        /// </summary>
        private static void AddParameters(
            IDbCommand command,
            object entity,
            EntityMetadata metadata,
            ParameterMode mode)
        {
            ValidateArguments(command, entity, metadata);

            foreach (ColumnMetadata column in metadata.Columns)
            {
                if (column == null)
                    continue;

                if (!ShouldInclude(column, mode))
                    continue;

                AddParameter(
                    command,
                    column,
                    entity);
            }
        }

        /// <summary>
        /// 지정한 컬럼을 DB Parameter로 추가합니다.
        /// </summary>
        private static void AddParameter(
            IDbCommand command,
            ColumnMetadata column,
            object entity)
        {
            if (column.PropertyInfo == null)
            {
                throw new InvalidOperationException(
                    "ColumnMetadata.PropertyInfo가 설정되지 않았습니다.");
            }

            object value = column.PropertyInfo.GetValue(
                entity,
                null);

            value = PrepareValue(
                value,
                column);

            IDbDataParameter parameter =
                command.CreateParameter();

            parameter.ParameterName =
                GetParameterName(column.ColumnName);

            parameter.Value =
                DbValueConverter.ConvertToDbValue(value);

            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// DB에 전달하기 전에 컬럼 속성에 맞게 값을 준비합니다.
        /// </summary>
        private static object PrepareValue(
            object value,
            ColumnMetadata column)
        {
            if (value == null ||
                value == DBNull.Value)
            {
                return DBNull.Value;
            }

            if (column.StoreDateOnly)
            {
                if (value is DateTime)
                {
                    return ((DateTime)value).Date;
                }

                if (value is DateTime?)
                {
                    DateTime? dateValue =
                        (DateTime?)value;

                    if (dateValue.HasValue)
                        return dateValue.Value.Date;

                    return DBNull.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// Parameter 이름을 생성합니다.
        /// </summary>
        private static string GetParameterName(
            string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentException(
                    "ColumnName이 비어 있습니다.",
                    nameof(columnName));
            }

            // Parameter 접두사는 SQL Dialect에서
            // 필요에 따라 처리할 수 있으므로 여기서는
            // 공통적으로 이름만 생성합니다.
            return "@" + columnName;
        }

        /// <summary>
        /// 현재 Parameter Mode에서 해당 컬럼을 포함할지 결정합니다.
        /// </summary>
        private static bool ShouldInclude(
            ColumnMetadata column,
            ParameterMode mode)
        {
            switch (mode)
            {
                case ParameterMode.Insert:
                    return column.IsInsertable;

                case ParameterMode.Update:
                    // UPDATE에서는 Primary Key를
                    // SET 값으로 사용하지 않습니다.
                    return !column.IsPrimaryKey &&
                           column.IsUpdatable;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        mode,
                        null);
            }
        }

        /// <summary>
        /// 전달된 인자를 검증합니다.
        /// </summary>
        private static void ValidateArguments(
            IDbCommand command,
            object entity,
            EntityMetadata metadata)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
        }

        #endregion

        #region ParameterMode

        /// <summary>
        /// Parameter 생성 모드입니다.
        /// </summary>
        private enum ParameterMode
        {
            Insert,
            Update
        }

        #endregion
    }
}