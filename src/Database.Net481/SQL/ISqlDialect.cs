using System;
using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.SQL
{
    /// <summary>
    /// 데이터베이스별 SQL 문법 차이를 추상화합니다.
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>
        /// 데이터베이스에서 사용하는 식별자(테이블명, 컬럼명 등)를
        /// 안전하게 감쌉니다.
        /// </summary>
        /// <param name="identifier">식별자입니다.</param>
        /// <returns>Dialect에 맞게 인용된 식별자입니다.</returns>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// SQL Parameter 이름을 생성합니다.
        /// </summary>
        /// <param name="name">Parameter 이름입니다.</param>
        /// <returns>Dialect에 맞는 Parameter 이름입니다.</returns>
        string GetParameterName(string name);

        /// <summary>
        /// INSERT SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름입니다.</param>
        /// <param name="columns">INSERT 대상 컬럼입니다.</param>
        /// <returns>INSERT SQL입니다.</returns>
        string BuildInsert(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns);

        /// <summary>
        /// UPDATE SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름입니다.</param>
        /// <param name="columns">UPDATE 대상 컬럼입니다.</param>
        /// <param name="primaryKeys">WHERE 조건에 사용할 Primary Key 컬럼입니다.</param>
        /// <returns>UPDATE SQL입니다.</returns>
        string BuildUpdate(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            IReadOnlyList<ColumnMetadata> primaryKeys);

        /// <summary>
        /// DELETE SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름입니다.</param>
        /// <param name="primaryKeys">WHERE 조건에 사용할 Primary Key 컬럼입니다.</param>
        /// <returns>DELETE SQL입니다.</returns>
        string BuildDelete(
            string tableName,
            IReadOnlyList<ColumnMetadata> primaryKeys);

        /// <summary>
        /// SELECT SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름입니다.</param>
        /// <param name="columns">SELECT 대상 컬럼입니다.</param>
        /// <param name="whereClause">선택적인 WHERE 조건입니다.</param>
        /// <returns>SELECT SQL입니다.</returns>
        string BuildSelect(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            string whereClause = null);

        /// <summary>
        /// Upsert SQL을 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름입니다.</param>
        /// <param name="columns">INSERT 대상 컬럼입니다.</param>
        /// <param name="primaryKeys">충돌 판단에 사용할 Primary Key 컬럼입니다.</param>
        /// <returns>Dialect에 맞는 Upsert SQL입니다.</returns>
        string BuildUpsert(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns,
            IReadOnlyList<ColumnMetadata> primaryKeys);
    }
}

