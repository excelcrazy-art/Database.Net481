using System.Data;

namespace Database.Net481.Database.Interfaces
{
    /// <summary>
    /// 데이터베이스 연결을 생성하는 Factory 인터페이스입니다.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// 새로운 데이터베이스 연결을 생성합니다.
        /// </summary>
        /// <returns>생성된 데이터베이스 연결</returns>
        IDbConnection CreateConnection();
    }
}