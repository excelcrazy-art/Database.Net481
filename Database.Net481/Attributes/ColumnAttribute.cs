using System;

namespace Database.Net481.Attributes
{
    /// <summary>
    /// 엔티티 속성과 데이터베이스 컬럼의 매핑 정보를 정의합니다.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 데이터베이스 컬럼 이름입니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Primary Key 여부입니다.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// INSERT 문에 포함할 수 있는지 여부입니다.
        /// </summary>
        public bool IsInsertable { get; set; } = true;

        /// <summary>
        /// UPDATE 문에 포함할 수 있는지 여부입니다.
        /// </summary>
        public bool IsUpdatable { get; set; } = true;

        /// <summary>
        /// 날짜 값을 날짜 부분만 저장할지 여부입니다.
        /// </summary>
        public bool StoreDateOnly { get; set; } = false;

        /// <summary>
        /// 컬럼 매핑 정보를 생성합니다.
        /// </summary>
        /// <param name="name">데이터베이스 컬럼 이름</param>
        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }
}

/*
사용예시

[Column("stock_code", IsKey = true)]
public string StockCode { get; set; }


[Column(
    "created_date",
    IsInsertable = true,
    IsUpdatable = false,
    StoreDateOnly = true)]
public DateTime CreatedDate { get; set; }
 */
