using System;

namespace Database.Net481.Attributes
{
    /// <summary>
    /// 데이터베이스 인덱스 정보를 정의합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class IndexAttribute : Attribute
    {
        /// <summary>
        /// 인덱스 이름입니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 인덱스를 구성하는 컬럼 이름들입니다.
        /// </summary>
        public string[] Columns { get; }

        /// <summary>
        /// UNIQUE 인덱스 여부입니다.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// 인덱스 정보를 생성합니다.
        /// </summary>
        /// <param name="name">인덱스 이름</param>
        /// <param name="columns">인덱스를 구성하는 컬럼 이름</param>
        public IndexAttribute(string name, params string[] columns)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("인덱스 이름은 비어 있을 수 없습니다.", nameof(name));

            if (columns == null || columns.Length == 0)
                throw new ArgumentException(
                    "인덱스를 구성하는 컬럼이 하나 이상 필요합니다.",
                    nameof(columns));

            Name = name;
            Columns = columns;
        }
    }
}

/*
사용 예:

[Table("StockDaily")]
[Index("IX_StockDaily_TradeDate", "stock_code", "trade_date")]
public class StockDaily
{
}

Unique 인덱스라면:

[Index(
    "UX_StockDaily_CodeDate",
    "stock_code",
    "trade_date",
    IsUnique = true)]

*/
