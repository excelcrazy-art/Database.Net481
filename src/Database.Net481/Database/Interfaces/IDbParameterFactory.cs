using System.Data;

namespace Database.Net481.Database.Interfaces
{
    /// <summary>
    /// 데이터베이스 Parameter 생성을 담당하는 Factory입니다.
    /// </summary>
    public interface IDbParameterFactory
    {
        /// <summary>
        /// 지정한 이름과 값으로 데이터베이스 Parameter를 생성합니다.
        /// </summary>
        /// <param name="name">
        /// Parameter 이름입니다. 예: @Id, @Name
        /// </param>
        /// <param name="value">
        /// Parameter 값입니다.
        /// null인 경우 DBNull.Value로 처리됩니다.
        /// </param>
        /// <returns>
        /// 생성된 IDbDataParameter입니다.
        /// </returns>
        IDbDataParameter CreateParameter(
            string name,
            object value);
    }
}
