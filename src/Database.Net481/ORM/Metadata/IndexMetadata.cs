namespace Database.Net481.ORM.Metadata
{
    /// <summary>
    /// 데이터베이스 인덱스의 메타데이터를 나타냅니다.
    /// </summary>
    public sealed class IndexMetadata
    {
        /// <summary>
        /// 인덱스 이름입니다.
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// 인덱스를 구성하는 컬럼 이름 목록입니다.
        /// </summary>
        public string[] ColumnNames { get; set; }

        /// <summary>
        /// UNIQUE 인덱스 여부입니다.
        /// </summary>
        public bool IsUnique { get; set; }

    }
}