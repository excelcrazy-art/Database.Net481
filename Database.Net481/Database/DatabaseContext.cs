using System;
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
        /// <param name="connectionFactory">
        /// 데이터베이스 연결을 생성하는 Factory입니다.
        /// </param>
        /// <param name="dialect">
        /// 데이터베이스별 SQL Dialect입니다.
        /// </param>
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
        /// <returns>
        /// 생성된 데이터베이스 연결입니다.
        /// </returns>
        public IDbConnection CreateConnection()
        {
            ThrowIfDisposed();

            return ConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// 데이터베이스 연결을 생성하고 Open 상태로 반환합니다.
        /// </summary>
        /// <returns>
        /// Open 상태의 데이터베이스 연결입니다.
        /// </returns>
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
        /// 객체가 Dispose된 이후 메서드가 호출되었는지 확인합니다.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatabaseContext));
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
