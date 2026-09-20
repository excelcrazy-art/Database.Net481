using System;
using System.Data;
using MySqlConnector;
using Database.Net481.Database.Interfaces;

namespace Database.Net481.MariaDb.Database
{
    /// <summary>
    /// MariaDB 데이터베이스 연결을 생성하는 Factory입니다.
    /// </summary>
    public sealed class MariaDbConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// MariaDB 연결 문자열입니다.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// MariaDbConnectionFactory를 생성합니다.
        /// </summary>
        /// <param name="connectionString">
        /// MariaDB 연결 문자열입니다.
        /// </param>
        public MariaDbConnectionFactory(string connectionString)
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
        /// 새로운 MariaDB 연결을 생성합니다.
        /// </summary>
        /// <returns>
        /// 생성된 IDbConnection입니다.
        /// </returns>
        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}