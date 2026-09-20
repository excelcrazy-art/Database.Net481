using System;

namespace Database.Net481.Attributes
{
    /// <summary>
    /// 엔티티와 데이터베이스 테이블의 매핑 정보를 정의합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// 테이블 이름입니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 테이블 매핑 정보를 생성합니다.
        /// </summary>
        /// <param name="name">테이블 이름</param>
        public TableAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("테이블 이름은 비어 있을 수 없습니다.", nameof(name));

            Name = name;
        }
    }
}

/*

    사용 예:
[Table("StockDaily")]
public class StockDaily
{
}

*/
