using System.Collections.Generic;
using System.Linq;

namespace Database.Net481.ORM.Metadata
{
    /// <summary>
    /// 엔티티의 테이블 및 컬럼 메타데이터를 나타냅니다.
    /// </summary>
    public sealed class EntityMetadata
    {
        /// <summary>
        /// 데이터베이스 테이블 이름입니다.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 엔티티의 컬럼 메타데이터 목록입니다.
        /// </summary>
        public List<ColumnMetadata> Columns { get; set; }

        /// <summary>
        /// Primary Key 컬럼 목록입니다.
        /// </summary>
        public List<ColumnMetadata> PrimaryKeys
        {
            get
            {
                return Columns
                    .Where(x => x.IsPrimaryKey)
                    .ToList();
            }
        }
    }
}