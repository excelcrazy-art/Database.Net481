using System;
using System.Collections.Generic;
using System.Data;
using Database.Net481.Database.Interfaces;
using Database.Net481.SQL;

namespace Database.Net481.Database
{
    /// <summary>
    /// 데이터베이스 연결 및 SQL Dialect를 관리하는 공통 데이터베이스 컨텍스트입니다.
    /// </summary>
    public sealed class DatabaseContext : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// 데이터베이스 연결을 생성하는 Factory입니다.
        /// </summary>
        public IDbConnectionFactory ConnectionFactory { get; }

        /// <summary>
        /// 현재 데이터베이스에서 사용할 SQL Dialect입니다.
        /// </summary>
        public ISqlDialect Dialect { get; }

        /// <summary>
        /// DatabaseContext를 생성합니다.
        /// </summary>
        public DatabaseContext(
            IDbConnectionFactory connectionFactory,
            ISqlDialect dialect)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            if (dialect == null)
            {
                throw new ArgumentNullException(nameof(dialect));
            }

            ConnectionFactory = connectionFactory;
            Dialect = dialect;
        }

        /// <summary>
        /// 새로운 데이터베이스 연결을 생성합니다.
        /// </summary>
        public IDbConnection CreateConnection()
        {
            ThrowIfDisposed();

            return ConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// 데이터베이스 연결을 생성하고 Open 상태로 반환합니다.
        /// </summary>
        public IDbConnection OpenConnection()
        {
            ThrowIfDisposed();

            IDbConnection connection = CreateConnection();

            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 새로운 데이터베이스 Transaction을 시작합니다.
        /// </summary>
        public DatabaseTransaction BeginTransaction()
        {
            ThrowIfDisposed();

            IDbConnection connection = OpenConnection();

            try
            {
                return new DatabaseTransaction(connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// SQL 명령을 실행하고 영향을 받은 행의 수를 반환합니다.
        /// </summary>
        public int ExecuteNonQuery(
            string sql,
            IEnumerable<IDbDataParameter> parameters = null,
            DatabaseTransaction transaction = null)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(
                    "SQL 문은 비어 있을 수 없습니다.",
                    nameof(sql));
            }

            if (transaction != null)
            {
                return ExecuteNonQuery(
                    transaction.Connection,
                    transaction.Transaction,
                    sql,
                    parameters);
            }

            using (IDbConnection connection = OpenConnection())
            {
                return ExecuteNonQuery(
                    connection,
                    null,
                    sql,
                    parameters);
            }
        }

        /// <summary>
        /// 지정된 연결과 Transaction을 사용하여 SQL 명령을 실행합니다.
        /// </summary>
        private int ExecuteNonQuery(
            IDbConnection connection,
            IDbTransaction transaction,
            string sql,
            IEnumerable<IDbDataParameter> parameters)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                AddParameters(command, parameters);

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// SQL 명령을 실행하고 첫 번째 행의 첫 번째 열을 반환합니다.
        /// </summary>
        /// <param name="sql">
        /// 실행할 SQL 문입니다.
        /// </param>
        /// <param name="parameters">
        /// SQL Parameter 목록입니다.
        /// </param>
        /// <param name="transaction">
        /// 사용할 DatabaseTransaction입니다.
        /// 지정하지 않으면 새 연결을 사용합니다.
        /// </param>
        /// <returns>
        /// 첫 번째 행의 첫 번째 열입니다.
        /// 결과가 없으면 null을 반환할 수 있습니다.
        /// </returns>
        public object ExecuteScalar(
            string sql,
            IEnumerable<IDbDataParameter> parameters = null,
            DatabaseTransaction transaction = null)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(
                    "SQL 문은 비어 있을 수 없습니다.",
                    nameof(sql));
            }

            if (transaction != null)
            {
                return ExecuteScalar(
                    transaction.Connection,
                    transaction.Transaction,
                    sql,
                    parameters);
            }

            using (IDbConnection connection = OpenConnection())
            {
                return ExecuteScalar(
                    connection,
                    null,
                    sql,
                    parameters);
            }
        }

        /// <summary>
        /// 지정된 연결과 Transaction을 사용하여 Scalar SQL 명령을 실행합니다.
        /// </summary>
        private object ExecuteScalar(
            IDbConnection connection,
            IDbTransaction transaction,
            string sql,
            IEnumerable<IDbDataParameter> parameters)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                AddParameters(command, parameters);

                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Command에 Parameter를 추가합니다.
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

        /// <summary>
        /// 객체가 Dispose된 이후 메서드가 호출되었는지 확인합니다.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(DatabaseContext));
            }
        }

        /// <summary>
        /// DatabaseContext에서 사용하는 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }
    }
}