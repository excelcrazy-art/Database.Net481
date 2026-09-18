using System.Reflection;

namespace Database.Net481.ORM.Metadata
{
    /// <summary>
    /// 엔티티 속성과 데이터베이스 컬럼의 메타데이터를 나타냅니다.
    /// </summary>
    public sealed class ColumnMetadata
    {
        /// <summary>
        /// 엔티티 속성의 Reflection 정보입니다.
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// 데이터베이스 컬럼 이름입니다.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Primary Key 여부입니다.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// INSERT 문에 포함할 수 있는지 여부입니다.
        /// </summary>
        public bool IsInsertable { get; set; }

        /// <summary>
        /// UPDATE 문에 포함할 수 있는지 여부입니다.
        /// </summary>
        public bool IsUpdatable { get; set; }

        /// <summary>
        /// 날짜 값을 날짜 부분만 저장할지 여부입니다.
        /// </summary>
        public bool StoreDateOnly { get; set; }
    }
}