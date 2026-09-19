namespace Database.Net481.Core
{
    /// <summary>
    /// 지원하는 데이터베이스 공급자를 정의합니다.
    /// </summary>
    public enum DatabaseProvider
    {
        /// <summary>
        /// SQLite
        /// </summary>
        SQLite = 0,

        /// <summary>
        /// MariaDB
        /// </summary>
        MariaDB = 1,

        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSQL = 2
    }
}

/*
 

Core
 └─ DatabaseOptions
       설정

Database
 └─ DatabaseContext
       연결/실행 컨텍스트

Database.Interfaces
 └─ IDbConnectionFactory
       연결 생성 계약

ORM
 └─ Metadata / Mapping / Cache
       객체 ↔ DB 매핑

SQL
 └─ ISqlDialect / Dialects
       DB별 SQL 생성 
    
 Database.Net481.sln
│
├─ Database.Net481
│  │
│  ├─ Attributes
│  │   ├─ TableAttribute.cs
│  │   ├─ ColumnAttribute.cs
│  │   ├─ KeyAttribute.cs(제외)
│  │   └─ IndexAttribute.cs
│  │
│  ├─ Core
│  │   ├─ DatabaseOptions.cs
│  │   ├─ DatabaseProvider.cs
│  │   ├─ DbContext.cs
│  │   └─ Interfaces
│  │       ├─ IDbConnectionFactory.cs
│  │       └─ ISqlDialect.cs
│  │
│  ├─ ORM
│  │   ├─ Metadata
│  │   │   ├─ EntityMetadata.cs
│  │   │   └─ ColumnMetadata.cs
│  │   │
│  │   ├─ Cache
│  │   │   └─ EntityMetadataCache.cs
│  │   │
│  │   ├─ Mapping
│  │   │   ├─ EntityMapper.cs
│  │   │   ├─ ParameterMapper.cs
│  │   │   └─ DataReaderMapper.cs
│  │   │
│  │   └─ Conversion
│  │       └─ DbValueConverter.cs
│  │
│  ├─ SQL
│  │   ├─ SelectSqlBuilder.cs
│  │   ├─ InsertSqlBuilder.cs
│  │   ├─ UpdateSqlBuilder.cs
│  │   ├─ DeleteSqlBuilder.cs
│  │   └─ UpsertSqlBuilder.cs
│  │
│  └─ Repositories
│      ├─ IRepository.cs
│      ├─ RepositoryBase.cs
│      ├─ QueryRepositoryBase.cs
│      └─ GenericRepository.cs
│
├─ Database.Net481.Sqlite
│  ├─ SqliteConnectionFactory.cs
│  ├─ SqliteDialect.cs
│  ├─ SqliteSchemaGenerator.cs
│  └─ SqlitePragmaManager.cs
│
├─ Database.Net481.MariaDb
│  ├─ MariaDbConnectionFactory.cs
│  ├─ MariaDbDialect.cs
│  └─ MariaDbSchemaGenerator.cs
│
├─ Database.Net481.PostgreSql
│  ├─ PostgreSqlConnectionFactory.cs
│  ├─ PostgreSqlDialect.cs
│  └─ PostgreSqlSchemaGenerator.cs
│
└─ Database.Net481.Sample



1차 개발 순서

STEP 1 — 공통 모델/Attribute
DatabaseProvider
DatabaseOptions
TableAttribute
ColumnAttribute
IndexAttribute

↓

STEP 2 — Metadata
EntityMetadata
ColumnMetadata
EntityMetadataCache

↓

STEP 3 — DB 추상화
IDbConnectionFactory
ISqlDialect
DbContext
Transaction

↓

STEP 4 — Mapping
ParameterMapper
DataReaderMapper
DbValueConverter
EntityMapper

↓

STEP 5 — SQL Builder
Select
Insert
Update
Delete
Upsert

↓

STEP 6 — Repository
Get
GetAll
Query
Insert
InsertRange
Update
Delete
DeleteWhere
Upsert
UpsertRange
Count
Exists
Execute
Scalar

↓

STEP 7 — SQLite

↓

STEP 8 — MariaDB

↓

STEP 9 — PostgreSQL

↓

STEP 10 — Sample/Test

     */
