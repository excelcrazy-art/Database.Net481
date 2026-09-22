using System;
using System.Data;
using System.Data.SQLite;
using Database.Net481.Database.Interfaces;

namespace Database.Net481.Sqlite.Database
{
    /// <summary>
    /// SQLite용 데이터베이스 Parameter Factory입니다.
    /// </summary>
    public sealed class SqliteParameterFactory : IDbParameterFactory
    {
        /// <summary>
        /// 지정한 이름과 값으로 SQLite Parameter를 생성합니다.
        /// </summary>
        /// <param name="name">
        /// Parameter 이름입니다. 예: @Id, @Name
        /// </param>
        /// <param name="value">
        /// Parameter 값입니다.
        /// null인 경우 DBNull.Value로 처리됩니다.
        /// </param>
        /// <returns>
        /// 생성된 SQLiteParameter입니다.
        /// </returns>
        public IDbDataParameter CreateParameter(
            string name,
            object value)
        {
            SQLiteParameter parameter =
                new SQLiteParameter(name);

            parameter.Value =
                value ?? DBNull.Value;

            return parameter;
        }
    }
}
