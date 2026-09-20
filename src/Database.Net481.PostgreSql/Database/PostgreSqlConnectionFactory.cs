using System;
using System.Data;
using Npgsql;
using Database.Net481.Database.Interfaces;

namespace Database.Net481.PostgreSql.Database
{
    /// <summary>
    /// PostgreSQL 데이터베이스 연결을 생성하는 Factory입니다.
    /// </summary>
    public sealed class PostgreSqlConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// PostgreSQL 연결 문자열입니다.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// PostgreSqlConnectionFactory를 생성합니다.
        /// </summary>
        /// <param name="connectionString">
        /// PostgreSQL 연결 문자열입니다.
        /// </param>
        public PostgreSqlConnectionFactory(string connectionString)
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
        /// 새로운 PostgreSQL 연결을 생성합니다.
        /// </summary>
        /// <returns>
        /// 생성된 IDbConnection입니다.
        /// </returns>
        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
    }
}