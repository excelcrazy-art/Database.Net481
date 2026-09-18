using System;

namespace Database.Net481.Core
{
    /// <summary>
    /// 데이터베이스 연결에 필요한 공통 옵션을 정의합니다.
    /// </summary>
    public sealed class DatabaseOptions
    {
        /// <summary>
        /// 사용할 데이터베이스 공급자입니다.
        /// </summary>
        public DatabaseProvider Provider { get; set; }

        /// <summary>
        /// 데이터베이스 연결 문자열입니다.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 연결 시도 제한 시간(초)입니다.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 데이터베이스 옵션을 생성합니다.
        /// </summary>
        public DatabaseOptions()
        {
            ConnectionString = string.Empty;
        }

        /// <summary>
        /// 필수 설정값을 검증합니다.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException(
                    "데이터베이스 연결 문자열이 지정되지 않았습니다.");

            if (ConnectionTimeout < 0)
                throw new InvalidOperationException(
                    "ConnectionTimeout은 0 이상이어야 합니다.");
        }
    }
}