using System.Data;

namespace Database.Net481.Database.Interfaces
{
    /// <summary>
    /// 데이터베이스 연결 생성을 추상화합니다.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// 데이터베이스 연결 문자열입니다.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// 데이터베이스 연결을 생성합니다.
        /// </summary>
        /// <returns>생성된 데이터베이스 연결</returns>
        IDbConnection CreateConnection();
    }
}