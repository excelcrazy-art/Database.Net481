using System.Collections.Generic;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.Schema
{
    /// <summary>
    /// 데이터베이스별 Schema 및 DDL 생성을 담당하는 인터페이스입니다.
    /// </summary>
    public interface ISchemaDialect
    {
        /// <summary>
        /// 식별자(Table명, Column명, Index명 등)를
        /// 데이터베이스에 맞는 형태로 인용합니다.
        /// </summary>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// .NET Property의 자료형을
        /// 데이터베이스의 Column 자료형으로 변환합니다.
        /// </summary>
        string GetColumnType(ColumnMetadata column);

        /// <summary>
        /// CREATE TABLE SQL을 생성합니다.
        /// </summary>
        string BuildCreateTable(
            string tableName,
            IReadOnlyList<ColumnMetadata> columns);

        /// <summary>
        /// CREATE INDEX SQL을 생성합니다.
        /// </summary>
        string BuildCreateIndex(
            string tableName,
            IndexMetadata index);

        /// <summary>
        /// DROP TABLE SQL을 생성합니다.
        /// </summary>
        string BuildDropTable(
            string tableName);

        /// <summary>
        /// DROP INDEX SQL을 생성합니다.
        /// </summary>
        string BuildDropIndex(
            string tableName,
            string indexName);
    }
}