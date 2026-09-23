using System;
using System.Collections.Generic;
using System.Data;
using Database.Net481.Database;
using Database.Net481.ORM.Cache;
using Database.Net481.ORM.Mapping;
using Database.Net481.ORM.Metadata;
using Database.Net481.SQL.Builders;

namespace Database.Net481.Repositories
{
    /// <summary>
    /// Entity에 대한 기본 데이터베이스 작업을 제공합니다.
    /// </summary>
    /// <typeparam name="T">Entity 타입입니다.</typeparam>
    public class GenericRepository<T>
        where T : new()
    {
        private readonly DatabaseContext _context;
        private readonly EntityMetadata _metadata;

        /// <summary>
        /// 사용할 DatabaseContext입니다.
        /// </summary>
        protected DatabaseContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// Entity Metadata입니다.
        /// </summary>
        protected EntityMetadata Metadata
        {
            get { return _metadata; }
        }

        /// <summary>
        /// GenericRepository를 생성합니다.
        /// </summary>
        /// <param name="context">
        /// 사용할 DatabaseContext입니다.
        /// </param>
        public GenericRepository(DatabaseContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
            _metadata = EntityMetadataCache.GetMetadata<T>();
        }

        #region Query

        /// <summary>
        /// SQL을 실행하고 결과를 Entity 목록으로 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <returns>
        /// 조회된 Entity 목록입니다.
        /// </returns>
        public List<T> Query(string sql)
        {
            return Query(
                sql,
                null,
                null);
        }

        /// <summary>
        /// SQL을 실행하고 결과를 Entity 목록으로 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <param name="parameters">
        /// SQL Parameter 목록입니다.
        /// </param>
        /// <returns>
        /// 조회된 Entity 목록입니다.
        /// </returns>
        public List<T> Query(
            string sql,
            IEnumerable<IDbDataParameter> parameters)
        {
            return Query(
                sql,
                parameters,
                null);
        }

        /// <summary>
        /// SQL을 실행하고 결과를 Entity 목록으로 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <param name="parameters">
        /// SQL Parameter 목록입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 조회된 Entity 목록입니다.
        /// </returns>
        public List<T> Query(
            string sql,
            IEnumerable<IDbDataParameter> parameters,
            DatabaseTransaction transaction)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(
                    "SQL 문은 비어 있을 수 없습니다.",
                    nameof(sql));
            }

            if (transaction != null)
            {
                return ExecuteQuery(
                    transaction.Connection,
                    transaction.Transaction,
                    sql,
                    parameters);
            }

            using (IDbConnection connection =
                   _context.OpenConnection())
            {
                return ExecuteQuery(
                    connection,
                    null,
                    sql,
                    parameters);
            }
        }

        /// <summary>
        /// 지정된 Connection과 Transaction을 사용하여
        /// SQL Query를 실행합니다.
        /// </summary>
        private List<T> ExecuteQuery(
            IDbConnection connection,
            IDbTransaction transaction,
            string sql,
            IEnumerable<IDbDataParameter> parameters)
        {
            using (IDbCommand command =
                   connection.CreateCommand())
            {
                command.CommandText = sql;

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                AddParameters(
                    command,
                    parameters);

                using (IDataReader reader =
                       command.ExecuteReader())
                {
                    return DataReaderMapper.MapList<T>(
                        reader,
                        _metadata);
                }
            }
        }

        #endregion

        #region Execute

        /// <summary>
        /// SQL 명령을 실행하고 영향을 받은 행의 수를 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <param name="parameters">
        /// SQL Parameter 목록입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행의 수입니다.
        /// </returns>
        public int Execute(
            string sql,
            IEnumerable<IDbDataParameter> parameters = null,
            DatabaseTransaction transaction = null)
        {
            return _context.ExecuteNonQuery(
                sql,
                parameters,
                transaction);
        }

        /// <summary>
        /// Entity를 데이터베이스에 추가합니다.
        /// </summary>
        /// <param name="entity">
        /// 추가할 Entity입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Insert(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            string sql =
                InsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbConnection connection = _context.OpenConnection())
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    ParameterMapper.AddInsertParameters(
                        command,
                        entity,
                        _metadata);

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Entity를 지정된 Transaction 내에서 데이터베이스에 추가합니다.
        /// </summary>
        /// <param name="entity">
        /// 추가할 Entity입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Insert(
            T entity,
            DatabaseTransaction transaction)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            string sql =
                InsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbCommand command =
                   transaction.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction.Transaction;

                ParameterMapper.AddInsertParameters(
                    command,
                    entity,
                    _metadata);

                return command.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// Primary Key를 기준으로 Entity를 조회합니다.
        /// 단일 Primary Key와 복합 Primary Key를 모두 지원합니다.
        /// </summary>
        /// <param name="keyValues">
        /// Primary Key 값입니다.
        /// 복합 Primary Key인 경우 Primary Key 정의 순서와 동일한 순서로 전달합니다.
        /// </param>
        /// <returns>
        /// 조회된 Entity입니다.
        /// 조회 결과가 없으면 기본값을 반환합니다.
        /// </returns>
        public T GetById(params object[] keyValues)
        {
            if (keyValues == null)
            {
                throw new ArgumentNullException(nameof(keyValues));
            }

            if (_metadata.PrimaryKeys == null ||
                _metadata.PrimaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "GetById를 사용하려면 Primary Key가 정의되어 있어야 합니다.");
            }

            if (keyValues.Length != _metadata.PrimaryKeys.Count)
            {
                throw new ArgumentException(
                    string.Format(
                        "Primary Key 값의 개수가 올바르지 않습니다. " +
                        "필요한 개수: {0}, 전달된 개수: {1}",
                        _metadata.PrimaryKeys.Count,
                        keyValues.Length),
                    nameof(keyValues));
            }

            string whereClause = BuildPrimaryKeyWhereClause();

            string sql =
                SelectSqlBuilder.Build(
                    _metadata,
                    _context.Dialect,
                    whereClause);

            using (IDbConnection connection = _context.OpenConnection())
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    AddPrimaryKeyParameters(
                        command,
                        keyValues);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return default(T);
                        }

                        return DataReaderMapper.Map<T>(
                            reader,
                            _metadata);
                    }
                }
            }
        }

        /// <summary>
        /// Entity를 수정합니다.
        /// Primary Key를 기준으로 수정 대상 행을 결정합니다.
        /// </summary>
        /// <param name="entity">
        /// 수정할 Entity입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            string sql =
                UpdateSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbConnection connection = _context.OpenConnection())
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    ParameterMapper.AddUpdateParameters(
                        command,
                        entity,
                        _metadata);

                    ParameterMapper.AddPrimaryKeyParameters(
                        command,
                        entity,
                        _metadata);

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Entity를 지정된 Transaction 내에서 데이터베이스에 수정합니다.
        /// </summary>
        /// <param name="entity">
        /// 수정할 Entity입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Update(
            T entity,
            DatabaseTransaction transaction)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            string sql =
                UpdateSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbCommand command =
                   transaction.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction.Transaction;

                ParameterMapper.AddUpdateParameters(
                    command,
                    entity,
                    _metadata);

                ParameterMapper.AddPrimaryKeyParameters(
                    command,
                    entity,
                    _metadata);

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Entity를 삭제합니다.
        /// Primary Key를 기준으로 삭제 대상 행을 결정합니다.
        /// </summary>
        /// <param name="entity">
        /// 삭제할 Entity입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Delete(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            string sql =
                DeleteSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbConnection connection = _context.OpenConnection())
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    ParameterMapper.AddPrimaryKeyParameters(
                        command,
                        entity,
                        _metadata);

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Entity를 지정된 Transaction 내에서 데이터베이스에서 삭제합니다.
        /// </summary>
        /// <param name="entity">
        /// 삭제할 Entity입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Delete(
            T entity,
            DatabaseTransaction transaction)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            string sql =
                DeleteSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbCommand command =
                   transaction.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction.Transaction;

                ParameterMapper.AddPrimaryKeyParameters(
                    command,
                    entity,
                    _metadata);

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Entity를 추가하거나 수정합니다.
        /// Primary Key가 존재하면 수정하고, 존재하지 않으면 추가합니다.
        /// </summary>
        /// <param name="entity">
        /// 추가 또는 수정할 Entity입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Upsert(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            string sql =
                UpsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbConnection connection = _context.OpenConnection())
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    ParameterMapper.AddInsertParameters(
                        command,
                        entity,
                        _metadata);

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Entity를 지정된 Transaction 내에서 데이터베이스에 추가하거나 수정합니다.
        /// </summary>
        /// <param name="entity">
        /// 추가하거나 수정할 Entity입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 영향을 받은 행 수입니다.
        /// </returns>
        public int Upsert(
            T entity,
            DatabaseTransaction transaction)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            string sql =
                UpsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            using (IDbCommand command =
                   transaction.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction.Transaction;

                ParameterMapper.AddInsertParameters(
                    command,
                    entity,
                    _metadata);

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 여러 Entity를 데이터베이스에 추가합니다.
        /// 모든 Entity는 하나의 Transaction으로 처리됩니다.
        /// </summary>
        /// <param name="entities">
        /// 추가할 Entity 목록입니다.
        /// </param>
        /// <returns>
        /// 실제로 영향을 받은 총 행 수입니다.
        /// </returns>
        public int InsertRange(IEnumerable<T> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            string sql =
                InsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            int affectedRows = 0;

            using (DatabaseTransaction transaction =
                _context.BeginTransaction())
            {
                using (IDbCommand command =
                    transaction.Connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Transaction = transaction.Transaction;

                    foreach (T entity in entities)
                    {
                        if (entity == null)
                        {
                            throw new ArgumentException(
                                "Entity 목록에 null 항목이 포함되어 있습니다.",
                                nameof(entities));
                        }

                        command.Parameters.Clear();

                        ParameterMapper.AddInsertParameters(
                            command,
                            entity,
                            _metadata);

                        affectedRows += command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }

            return affectedRows;
        }

        /// <summary>
        /// 여러 Entity를 추가하거나 수정합니다.
        /// 모든 Entity는 하나의 Transaction으로 처리됩니다.
        /// </summary>
        /// <param name="entities">
        /// 추가 또는 수정할 Entity 목록입니다.
        /// </param>
        /// <returns>
        /// 실제로 영향을 받은 총 행 수입니다.
        /// </returns>
        public int UpsertRange(IEnumerable<T> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            string sql =
                UpsertSqlBuilder.Build(
                    _metadata,
                    _context.Dialect);

            int affectedRows = 0;

            using (DatabaseTransaction transaction =
                _context.BeginTransaction())
            {
                using (IDbCommand command =
                    transaction.Connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Transaction = transaction.Transaction;

                    foreach (T entity in entities)
                    {
                        if (entity == null)
                        {
                            throw new ArgumentException(
                                "Entity 목록에 null 항목이 포함되어 있습니다.",
                                nameof(entities));
                        }

                        command.Parameters.Clear();

                        ParameterMapper.AddInsertParameters(
                            command,
                            entity,
                            _metadata);

                        affectedRows += command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }

            return affectedRows;
        }

        /// <summary>
        /// Entity의 전체 행 수를 반환합니다.
        /// </summary>
        /// <returns>
        /// 전체 행 수입니다.
        /// </returns>
        public long Count()
        {
            return Count(null, null);
        }

        /// <summary>
        /// 조건에 해당하는 Entity의 행 수를 반환합니다.
        /// </summary>
        /// <param name="whereClause">
        /// WHERE 조건입니다.
        /// 예: "Price &gt; @Price"
        /// </param>
        /// <param name="parameters">
        /// WHERE 조건에 사용할 Parameters입니다.
        /// </param>
        /// <returns>
        /// 조건에 해당하는 행 수입니다.
        /// </returns>
        public long Count(
            string whereClause,
            IEnumerable<IDbDataParameter> parameters)
        {
            string sql =
                BuildCountSql(whereClause);

            object result =
                _context.ExecuteScalar(
                    sql,
                    parameters);

            if (result == null || result == DBNull.Value)
            {
                return 0L;
            }

            return Convert.ToInt64(result);
        }

        /*
        사용 예

        전체 개수:

        long count = repository.Count();

        조건부 개수:

        using (IDbConnection connection = context.OpenConnection())
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "...";
            }
        }

        처럼 직접 Command를 만들 필요 없이, 현재 DatabaseContext의 Parameter 구조를 이용할 수 있습니다.

        예를 들어:

        IDbDataParameter parameter =
            connection.CreateCommand().CreateParameter();

        parameter.ParameterName = "@Price";
        parameter.Value = 1000;

        long count =
            repository.Count(
                "Price >= @Price",
                new[] { parameter });

        다만 위 예시는 Parameter를 만들기 위해 Connection을 직접 열어야 해서 사용성이 좋지 않습니다.

        그래서 이 부분은 다음 단계에서 개선하는 것이 좋습니다.

        현재 Count()/Exists() API는 IEnumerable<IDbDataParameter>를 받도록 했지만, 
        일반 사용자가 Parameter를 직접 만들게 하면 Repository abstraction의 장점이 조금 떨어집니다.

        */

        /// <summary>
        /// 조건에 해당하는 Entity가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="whereClause">
        /// WHERE 조건입니다.
        /// 예: "StockCode = @StockCode"
        /// </param>
        /// <param name="parameters">
        /// WHERE 조건에 사용할 Parameters입니다.
        /// </param>
        /// <returns>
        /// 조건에 해당하는 Entity가 하나 이상 존재하면 true입니다.
        /// </returns>
        public bool Exists(
            string whereClause,
            IEnumerable<IDbDataParameter> parameters)
        {
            return Count(
                whereClause,
                parameters) > 0;
        }

        /// <summary>
        /// 지정한 조건에 해당하는 Entity를 삭제합니다.
        /// </summary>
        /// <param name="whereClause">
        /// WHERE 조건입니다.
        /// 예: "Price &lt; @Price"
        /// </param>
        /// <param name="parameters">
        /// WHERE 조건에 사용할 Parameters입니다.
        /// </param>
        /// <returns>
        /// 삭제된 행 수입니다.
        /// </returns>
        public int DeleteWhere(
            string whereClause,
            IEnumerable<IDbDataParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(whereClause))
            {
                throw new ArgumentException(
                    "WHERE 조건은 비어 있을 수 없습니다.",
                    nameof(whereClause));
            }

            if (string.IsNullOrWhiteSpace(_metadata.TableName))
            {
                throw new InvalidOperationException(
                    "Entity Metadata의 TableName이 비어 있습니다.");
            }

            string condition = whereClause.Trim();

            if (condition.StartsWith(
                    "WHERE ",
                    StringComparison.OrdinalIgnoreCase))
            {
                condition = condition.Substring(6).Trim();
            }

            if (string.IsNullOrWhiteSpace(condition))
            {
                throw new ArgumentException(
                    "WHERE 조건은 비어 있을 수 없습니다.",
                    nameof(whereClause));
            }

            string sql =
                "DELETE FROM "
                + _context.Dialect.QuoteIdentifier(
                    _metadata.TableName)
                + " WHERE "
                + condition;

            return _context.ExecuteNonQuery(
                sql,
                parameters);
        }

        /*
        DeleteWhere()는 Count()와 마찬가지로:

        IEnumerable<IDbDataParameter>

        를 외부에서 받아야 합니다.

        즉 호출하는 쪽에서 IDbDataParameter를 만들어야 합니다.

        이것은 현재 v1 구조에서는 정상적인 상태입니다. 다만 앞으로 Repository API를 실제 사용하기 편하게 만들 때는 개선할 여지가 있습니다.

        그리고 SQL Injection 방지를 위해 whereClause 자체에 사용자 입력값을 문자열로 붙이지 않고:

        Price < @Price

        처럼 파라미터를 사용하는 방식을 유지해야 합니다.
         */

        /// <summary>
        /// SQL을 실행하고 첫 번째 행의 첫 번째 열을 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <param name="parameters">
        /// SQL Parameter 목록입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// </param>
        /// <returns>
        /// 첫 번째 행의 첫 번째 열입니다.
        /// </returns>
        public object Scalar(
            string sql,
            IEnumerable<IDbDataParameter> parameters = null,
            DatabaseTransaction transaction = null)
        {
            return _context.ExecuteScalar(
                sql,
                parameters,
                transaction);
        }

        #endregion

        #region Parameter

        /// <summary>
        /// Command에 SQL Parameter를 추가합니다.
        /// </summary>
        private static void AddParameters(
            IDbCommand command,
            IEnumerable<IDbDataParameter> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (IDbDataParameter parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                command.Parameters.Add(parameter);
            }
        }

        #endregion

        /// <summary>
        /// Primary Key를 이용한 WHERE 조건을 생성합니다.
        /// </summary>
        private string BuildPrimaryKeyWhereClause()
        {
            List<string> conditions =
                new List<string>();

            foreach (ColumnMetadata column in _metadata.PrimaryKeys)
            {
                if (column == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(column.ColumnName))
                {
                    throw new InvalidOperationException(
                        "Primary Key Column의 ColumnName이 비어 있습니다.");
                }

                conditions.Add(
                    _context.Dialect.QuoteIdentifier(column.ColumnName)
                    + " = "
                    + _context.Dialect.GetParameterName(column.ColumnName));
            }

            if (conditions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Primary Key가 정의되어 있지 않습니다.");
            }

            return string.Join(
                " AND ",
                conditions);
        }

        /// <summary>
        /// Primary Key 값을 Command Parameter로 추가합니다.
        /// </summary>
        private void AddPrimaryKeyParameters(
            IDbCommand command,
            object[] keyValues)
        {
            for (int i = 0;
                 i < _metadata.PrimaryKeys.Count;
                 i++)
            {
                ColumnMetadata column =
                    _metadata.PrimaryKeys[i];

                IDbDataParameter parameter =
                    command.CreateParameter();

                parameter.ParameterName =
                    _context.Dialect.GetParameterName(
                        column.ColumnName);

                parameter.Value =
                    DbValueConverter.ConvertToDbValue(
                        keyValues[i]);

                command.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// COUNT SQL을 생성합니다.
        /// </summary>
        private string BuildCountSql(
            string whereClause)
        {
            if (string.IsNullOrWhiteSpace(_metadata.TableName))
            {
                throw new InvalidOperationException(
                    "Entity Metadata의 TableName이 비어 있습니다.");
            }

            string tableName =
                _context.Dialect.QuoteIdentifier(
                    _metadata.TableName);

            string sql =
                "SELECT COUNT(*) FROM " + tableName;

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                string condition =
                    whereClause.Trim();

                if (condition.StartsWith(
                        "WHERE ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    condition = condition.Substring(6).Trim();
                }

                if (!string.IsNullOrWhiteSpace(condition))
                {
                    sql += " WHERE " + condition;
                }
            }

            return sql;
        }

    }
}