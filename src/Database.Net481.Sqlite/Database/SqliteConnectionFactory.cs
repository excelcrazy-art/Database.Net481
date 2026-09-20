using System;
using System.Data;
using System.Data.SQLite;
using Database.Net481.Database.Interfaces;

namespace Database.Net481.Sqlite.Database
{
    /// <summary>
    /// SQLite 데이터베이스 연결을 생성합니다.
    /// </summary>
    public sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// SQLite 연결 문자열입니다.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// SQLite 연결 팩토리를 생성합니다.
        /// </summary>
        /// <param name="connectionString">
        /// SQLite 연결 문자열
        /// </param>
        public SqliteConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "연결 문자열은 비어 있을 수 없습니다.",
                    nameof(connectionString));
            }

            ConnectionString = connectionString;
        }

        /// <summary>
        /// SQLite 데이터베이스 연결을 생성합니다.
        /// </summary>
        /// <returns>생성된 SQLite 연결</returns>
        public IDbConnection CreateConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }
    }
}