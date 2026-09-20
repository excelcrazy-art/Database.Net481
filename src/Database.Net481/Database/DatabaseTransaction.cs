using System;
using System.Data;

namespace Database.Net481.Database
{
    /// <summary>
    /// 데이터베이스 Transaction을 관리합니다.
    /// </summary>
    public sealed class DatabaseTransaction : IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private bool _completed;
        private bool _disposed;

        /// <summary>
        /// Transaction이 사용하는 데이터베이스 연결입니다.
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                ThrowIfDisposed();
                return _connection;
            }
        }

        /// <summary>
        /// 실제 ADO.NET Transaction입니다.
        /// </summary>
        public IDbTransaction Transaction
        {
            get
            {
                ThrowIfDisposed();
                return _transaction;
            }
        }

        /// <summary>
        /// Transaction이 이미 Commit 또는 Rollback 되었는지 나타냅니다.
        /// </summary>
        public bool IsCompleted
        {
            get { return _completed; }
        }

        /// <summary>
        /// DatabaseTransaction을 생성합니다.
        /// </summary>
        /// <param name="connection">
        /// Transaction에 사용할 Open 상태의 데이터베이스 연결입니다.
        /// </param>
        public DatabaseTransaction(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "Transaction을 시작하려면 데이터베이스 연결이 Open 상태여야 합니다.");
            }

            _connection = connection;
            _transaction = connection.BeginTransaction();
        }

        /// <summary>
        /// Transaction을 Commit합니다.
        /// </summary>
        public void Commit()
        {
            ThrowIfDisposed();
            ThrowIfCompleted();

            _transaction.Commit();
            _completed = true;
        }

        /// <summary>
        /// Transaction을 Rollback합니다.
        /// </summary>
        public void Rollback()
        {
            ThrowIfDisposed();
            ThrowIfCompleted();

            _transaction.Rollback();
            _completed = true;
        }

        /// <summary>
        /// Transaction을 종료하고 연결을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!_completed)
                {
                    _transaction.Rollback();
                    _completed = true;
                }
            }
            finally
            {
                _transaction.Dispose();
                _connection.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// 객체가 Dispose되었는지 확인합니다.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(DatabaseTransaction));
            }
        }

        /// <summary>
        /// Transaction이 이미 완료되었는지 확인합니다.
        /// </summary>
        private void ThrowIfCompleted()
        {
            if (_completed)
            {
                throw new InvalidOperationException(
                    "이미 완료된 Transaction입니다.");
            }
        }
    }
}
