using System;
using Database.Net481.Database;
using Database.Net481.ORM.Cache;
using Database.Net481.ORM.Metadata;

namespace Database.Net481.Schema
{
    /// <summary>
    /// Entity Metadata를 기반으로 데이터베이스 Schema를 생성하고 관리합니다.
    /// </summary>
    public sealed class SchemaGenerator
    {
        private readonly DatabaseContext _context;
        private readonly ISchemaDialect _dialect;

        public SchemaGenerator(
            DatabaseContext context,
            ISchemaDialect dialect)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (dialect == null)
                throw new ArgumentNullException(nameof(dialect));

            _context = context;
            _dialect = dialect;
        }

        /// <summary>
        /// 지정한 Entity의 Table을 생성합니다.
        /// </summary>
        public string CreateTable<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            string sql =
                _dialect.BuildCreateTable(
                    metadata.TableName,
                    metadata.Columns);

            _context.ExecuteNonQuery(sql);

            return sql;
        }

        /// <summary>
        /// 지정한 Entity의 Index를 생성합니다.
        /// </summary>
        public int CreateIndexes<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            if (metadata.Indexes == null ||
                metadata.Indexes.Count == 0)
            {
                return 0;
            }

            int createdCount = 0;

            foreach (IndexMetadata index in metadata.Indexes)
            {
                if (index == null)
                    continue;

                string sql =
                    _dialect.BuildCreateIndex(
                        metadata.TableName,
                        index);

                _context.ExecuteNonQuery(sql);

                createdCount++;
            }

            return createdCount;
        }

        /// <summary>
        /// 지정한 Entity의 Table과 Index를 생성합니다.
        /// </summary>
        public int Create<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            string tableSql =
                _dialect.BuildCreateTable(
                    metadata.TableName,
                    metadata.Columns);

            _context.ExecuteNonQuery(tableSql);

            int count = 1;

            if (metadata.Indexes == null)
                return count;

            foreach (IndexMetadata index in metadata.Indexes)
            {
                if (index == null)
                    continue;

                string indexSql =
                    _dialect.BuildCreateIndex(
                        metadata.TableName,
                        index);

                _context.ExecuteNonQuery(indexSql);

                count++;
            }

            return count;
        }

        /// <summary>
        /// 지정한 Entity의 Table을 삭제합니다.
        /// </summary>
        public string DropTable<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            string sql =
                _dialect.BuildDropTable(
                    metadata.TableName);

            _context.ExecuteNonQuery(sql);

            return sql;
        }

        /// <summary>
        /// 지정한 Entity의 Index를 삭제합니다.
        /// </summary>
        public int DropIndexes<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            if (metadata.Indexes == null ||
                metadata.Indexes.Count == 0)
            {
                return 0;
            }

            int droppedCount = 0;

            foreach (IndexMetadata index in metadata.Indexes)
            {
                if (index == null)
                    continue;

                string sql =
                    _dialect.BuildDropIndex(
                        metadata.TableName,
                        index.IndexName);

                _context.ExecuteNonQuery(sql);

                droppedCount++;
            }

            return droppedCount;
        }

        /// <summary>
        /// 지정한 Entity의 Table과 Index를 삭제합니다.
        /// </summary>
        public int Drop<T>()
        {
            EntityMetadata metadata =
                EntityMetadataCache.GetMetadata<T>();

            int count = 0;

            if (metadata.Indexes != null)
            {
                foreach (IndexMetadata index in metadata.Indexes)
                {
                    if (index == null)
                        continue;

                    string sql =
                        _dialect.BuildDropIndex(
                            metadata.TableName,
                            index.IndexName);

                    _context.ExecuteNonQuery(sql);

                    count++;
                }
            }

            string tableSql =
                _dialect.BuildDropTable(
                    metadata.TableName);

            _context.ExecuteNonQuery(tableSql);

            count++;

            return count;
        }
    }
}