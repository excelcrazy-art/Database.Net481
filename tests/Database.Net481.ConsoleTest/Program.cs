using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Database.Net481.Attributes;
using Database.Net481.Core;
using Database.Net481.Database;
using Database.Net481.ORM.Cache;
using Database.Net481.ORM.Metadata;
using Database.Net481.Repositories;
using Database.Net481.Schema;
using Database.Net481.Schema.Dialects;
using Database.Net481.SQL.Builders;
using Database.Net481.SQL.Dialects;
using Database.Net481.Sqlite.Database;

namespace Database.Net481.ConsoleTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" Database.Net481 SQLite Test");
            Console.WriteLine("========================================");
            Console.WriteLine();

            string databasePath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "test.db");

            string connectionString =
                "Data Source=" + databasePath + ";Version=3;";

            Console.WriteLine("Database : " + databasePath);
            Console.WriteLine();

            try
            {
                TestSchema(connectionString);
                TestExecuteReader(connectionString);
                TestRepository(connectionString);
                TestTransaction(connectionString);
                TestTransactionException(connectionString);

                TestGenericRepositoryTransaction(connectionString);
                TestOrmMapping(connectionString);

                TestSqlBuilder(connectionString);

                TestGenericRepositoryConditions(connectionString);

                TestParameterFactory(connectionString);

                TestDatabaseOptions();

                TestDatabaseContextDispose(connectionString);

                TestDatabaseContextSqlExceptions(connectionString);

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" TEST PASS");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" TEST FAIL");
                Console.WriteLine("========================================");
                Console.WriteLine();

                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("계속하려면 아무 키나 누르십시오...");
            Console.ReadKey();
        }

        #region Existing Tests


        private static void TestSchema(string connectionString)
        {
            Console.WriteLine("[1] SQLite 연결 객체 생성");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            Console.WriteLine("    OK");

            Console.WriteLine();
            Console.WriteLine("[2] SchemaGenerator 생성");

            SqliteSchemaDialect schemaDialect =
                new SqliteSchemaDialect();

            SchemaGenerator schemaGenerator =
                new SchemaGenerator(
                    context,
                    schemaDialect);

            Console.WriteLine("    OK");

            Console.WriteLine();
            Console.WriteLine("[3] TestUser 테이블 생성");

            int createdCount =
                schemaGenerator.Create<TestUser>();

            Console.WriteLine(
                "    생성된 객체 수 : " + createdCount);

            Console.WriteLine();
            Console.WriteLine("[4] TestUser 테이블 삭제");

            int droppedCount =
                schemaGenerator.Drop<TestUser>();

            Console.WriteLine(
                "    삭제된 객체 수 : " + droppedCount);

            context.Dispose();
        }


        private static void TestExecuteReader(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[5] ExecuteReader 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            try
            {
                Console.WriteLine("    TestUser 테이블 생성");

                context.ExecuteNonQuery(
                    @"CREATE TABLE TestUser
      (
          Id   INTEGER PRIMARY KEY AUTOINCREMENT,
          Name TEXT NOT NULL,
          Age  INTEGER NOT NULL
      )");

                Console.WriteLine("    OK");

                Console.WriteLine();
                Console.WriteLine("    테스트 데이터 입력");

                context.ExecuteNonQuery(
                    @"INSERT INTO TestUser (Name, Age)
      VALUES ('홍길동', 30)");

                context.ExecuteNonQuery(
                    @"INSERT INTO TestUser (Name, Age)
      VALUES ('김철수', 40)");

                context.ExecuteNonQuery(
                    @"INSERT INTO TestUser (Name, Age)
      VALUES ('이영희', 50)");

                Console.WriteLine("    OK");

                Console.WriteLine();
                Console.WriteLine("    전체 데이터 조회");

                int count = 0;

                using (IDataReader reader =
                    context.ExecuteReader(
                        @"SELECT Id, Name, Age
          FROM TestUser
          ORDER BY Id"))
                {
                    while (reader.Read())
                    {
                        int id =
                            Convert.ToInt32(reader["Id"]);

                        string name =
                            Convert.ToString(reader["Name"]);

                        int age =
                            Convert.ToInt32(reader["Age"]);

                        Console.WriteLine(
                            "        Id={0}, Name={1}, Age={2}",
                            id,
                            name,
                            age);

                        count++;
                    }
                }

                if (count != 3)
                {
                    throw new Exception(
                        "조회된 행 수가 예상과 다릅니다. " +
                        "예상: 3, 실제: " + count);
                }

                Console.WriteLine(
                    "    OK - 조회된 행 수 : " + count);

                Console.WriteLine();
                Console.WriteLine("    TestUser 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE TestUser");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " + droppedCount);

                Console.WriteLine();
                Console.WriteLine("    ExecuteReader 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }

        #endregion

        #region Repository Test

        private static void TestRepository(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[6] GenericRepository CRUD 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            try
            {
                PrepareRepositoryTable(context);

                GenericRepository<TestUser> repository =
                    new GenericRepository<TestUser>(context);

                TestRepositoryInsert(repository);

                TestRepositoryGetById(repository);

                TestRepositoryUpdate(repository);

                TestRepositoryQuery(repository);

                TestRepositoryCount(repository);

                TestRepositoryExists(repository);

                TestRepositoryDelete(repository);

                TestRepositoryInsertRange(repository);

                TestRepositoryUpsert(repository);

                TestRepositoryUpsertRange(repository);

                TestRepositoryDeleteWhere(repository);

                Console.WriteLine();
                Console.WriteLine("    GenericRepository 테스트 PASS");
            }
            finally
            {
                try
                {
                    context.ExecuteNonQuery(
                        "DROP TABLE IF EXISTS TestUser");
                }
                catch
                {
                    // 테스트 정리 실패가 원래 예외를 가리지 않도록 합니다.
                }

                context.Dispose();
            }
        }

        private static void PrepareRepositoryTable(
            DatabaseContext context)
        {
            Console.WriteLine();
            Console.WriteLine("    TestUser 테이블 준비");

            context.ExecuteNonQuery(
                "DROP TABLE IF EXISTS TestUser");

            context.ExecuteNonQuery(
                @"CREATE TABLE TestUser
          (
              Id   INTEGER PRIMARY KEY AUTOINCREMENT,
              Name TEXT NOT NULL,
              Age  INTEGER NOT NULL
          )");

            Console.WriteLine("    OK");
        }

        private static void TestRepositoryInsert(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-1] Insert");

            TestUser user =
                new TestUser
                {
                    Name = "홍길동",
                    Age = 30
                };

            int affectedRows =
                repository.Insert(user);

            if (affectedRows != 1)
            {
                throw new Exception(
                    "Insert 결과가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + affectedRows);
            }

            long count =
                repository.Count();

            if (count != 1)
            {
                throw new Exception(
                    "Insert 후 Count가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + count);
            }

            Console.WriteLine(
                "        Insert affected rows : " +
                affectedRows);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryGetById(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-2] GetById");

            TestUser insertedUser =
                GetFirstUser(repository);

            if (insertedUser == null)
            {
                throw new Exception(
                    "GetById 테스트용 Entity를 찾을 수 없습니다.");
            }

            TestUser user =
                repository.GetById(
                    insertedUser.Id);

            if (user == null)
            {
                throw new Exception(
                    "GetById 결과가 null입니다.");
            }

            if (user.Id != insertedUser.Id)
            {
                throw new Exception(
                    "GetById의 Id가 예상과 다릅니다.");
            }

            if (user.Name != insertedUser.Name)
            {
                throw new Exception(
                    "GetById의 Name이 예상과 다릅니다.");
            }

            if (user.Age != insertedUser.Age)
            {
                throw new Exception(
                    "GetById의 Age가 예상과 다릅니다.");
            }

            Console.WriteLine(
                "        Id={0}, Name={1}, Age={2}",
                user.Id,
                user.Name,
                user.Age);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryUpdate(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-3] Update");

            TestUser user =
                GetFirstUser(repository);

            if (user == null)
            {
                throw new Exception(
                    "Update 대상 Entity를 찾을 수 없습니다.");
            }

            user.Name = "홍길동 수정";
            user.Age = 35;

            int affectedRows =
                repository.Update(user);

            if (affectedRows != 1)
            {
                throw new Exception(
                    "Update 결과가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + affectedRows);
            }

            TestUser updated =
                repository.GetById(user.Id);

            if (updated == null)
            {
                throw new Exception(
                    "Update 후 Entity를 찾을 수 없습니다.");
            }

            if (updated.Name != "홍길동 수정")
            {
                throw new Exception(
                    "Update 후 Name이 변경되지 않았습니다.");
            }

            if (updated.Age != 35)
            {
                throw new Exception(
                    "Update 후 Age가 변경되지 않았습니다.");
            }

            Console.WriteLine(
                "        Id={0}, Name={1}, Age={2}",
                updated.Id,
                updated.Name,
                updated.Age);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryQuery(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-4] Query");

            List<TestUser> users =
                repository.Query(
                    @"SELECT Id, Name, Age
              FROM TestUser
              ORDER BY Id");

            if (users == null)
            {
                throw new Exception(
                    "Query 결과가 null입니다.");
            }

            if (users.Count != 1)
            {
                throw new Exception(
                    "Query 결과 개수가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + users.Count);
            }

            Console.WriteLine(
                "        조회된 Entity 수 : " +
                users.Count);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryCount(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-5] Count");

            long count =
                repository.Count();

            if (count != 1)
            {
                throw new Exception(
                    "Count 결과가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + count);
            }

            Console.WriteLine(
                "        전체 Entity 수 : " +
                count);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryExists(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-6] Exists");

            bool exists =
                repository.Exists(
                    "Age >= 30",
                    null);

            if (!exists)
            {
                throw new Exception(
                    "Exists 결과가 true여야 합니다.");
            }

            Console.WriteLine(
                "        Age >= 30 : " +
                exists);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryDelete(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-7] Delete");

            TestUser user =
                GetFirstUser(repository);

            if (user == null)
            {
                throw new Exception(
                    "Delete 대상 Entity를 찾을 수 없습니다.");
            }

            int affectedRows =
                repository.Delete(user);

            if (affectedRows != 1)
            {
                throw new Exception(
                    "Delete 결과가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + affectedRows);
            }

            TestUser deleted =
                repository.GetById(user.Id);

            if (deleted != null)
            {
                throw new Exception(
                    "Delete 후 Entity가 존재합니다.");
            }

            long count =
                repository.Count();

            if (count != 0)
            {
                throw new Exception(
                    "Delete 후 Count가 예상과 다릅니다. " +
                    "예상: 0, 실제: " + count);
            }

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryInsertRange(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-8] InsertRange");

            List<TestUser> users =
                new List<TestUser>
                {
            new TestUser
            {
                Name = "김철수",
                Age = 40
            },

            new TestUser
            {
                Name = "이영희",
                Age = 50
            },

            new TestUser
            {
                Name = "박민수",
                Age = 60
            }
                };

            int affectedRows =
                repository.InsertRange(users);

            if (affectedRows != 3)
            {
                throw new Exception(
                    "InsertRange 결과가 예상과 다릅니다. " +
                    "예상: 3, 실제: " + affectedRows);
            }

            long count =
                repository.Count();

            if (count != 3)
            {
                throw new Exception(
                    "InsertRange 후 Count가 예상과 다릅니다. " +
                    "예상: 3, 실제: " + count);
            }

            Console.WriteLine(
                "        InsertRange affected rows : " +
                affectedRows);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryUpsert(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-9] Upsert");

            /*
             * 현재 TestUser의 Id는
             *
             * IsPrimaryKey = true
             * IsInsertable = false
             *
             * 로 정의되어 있습니다.
             *
             * 따라서 현재 Upsert 구현이 자동 증가 PK를 이용하여
             * 기존 행을 수정하는지는 별도로 검증해야 합니다.
             *
             * 이번 테스트에서는 현재 API가 실제로 실행되고
             * 영향을 받은 행 수가 정상인지 확인합니다.
             */

            TestUser user =
                new TestUser
                {
                    Name = "최영수",
                    Age = 70
                };

            int affectedRows =
                repository.Upsert(user);

            if (affectedRows != 1)
            {
                throw new Exception(
                    "Upsert 결과가 예상과 다릅니다. " +
                    "예상: 1, 실제: " + affectedRows);
            }

            long count =
                repository.Count();

            if (count != 4)
            {
                throw new Exception(
                    "Upsert 후 Count가 예상과 다릅니다. " +
                    "예상: 4, 실제: " + count);
            }

            Console.WriteLine(
                "        Upsert affected rows : " +
                affectedRows);

            Console.WriteLine(
                "        전체 Entity 수 : " +
                count);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryUpsertRange(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-10] UpsertRange");

            List<TestUser> users =
                new List<TestUser>
                {
            new TestUser
            {
                Name = "강민수",
                Age = 80
            },

            new TestUser
            {
                Name = "정민지",
                Age = 90
            }
                };

            int affectedRows =
                repository.UpsertRange(users);

            if (affectedRows != 2)
            {
                throw new Exception(
                    "UpsertRange 결과가 예상과 다릅니다. " +
                    "예상: 2, 실제: " + affectedRows);
            }

            long count =
                repository.Count();

            if (count != 6)
            {
                throw new Exception(
                    "UpsertRange 후 Count가 예상과 다릅니다. " +
                    "예상: 6, 실제: " + count);
            }

            Console.WriteLine(
                "        UpsertRange affected rows : " +
                affectedRows);

            Console.WriteLine(
                "        전체 Entity 수 : " +
                count);

            Console.WriteLine("        OK");
        }

        private static void TestRepositoryDeleteWhere(
            GenericRepository<TestUser> repository)
        {
            Console.WriteLine();
            Console.WriteLine("    [6-11] DeleteWhere");

            int affectedRows =
                repository.DeleteWhere(
                    "Age >= 80",
                    null);

            if (affectedRows != 2)
            {
                throw new Exception(
                    "DeleteWhere 결과가 예상과 다릅니다. " +
                    "예상: 2, 실제: " + affectedRows);
            }

            long count =
                repository.Count();

            if (count != 4)
            {
                throw new Exception(
                    "DeleteWhere 후 Count가 예상과 다릅니다. " +
                    "예상: 4, 실제: " + count);
            }

            Console.WriteLine(
                "        DeleteWhere affected rows : " +
                affectedRows);

            Console.WriteLine(
                "        남은 Entity 수 : " +
                count);

            Console.WriteLine("        OK");
        }

        #endregion

        
        private static void TestTransaction(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[7] DatabaseTransaction 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            try
            {
                Console.WriteLine();
                Console.WriteLine("    TestTransaction 테이블 생성");

                context.ExecuteNonQuery(
                    @"CREATE TABLE TestTransaction
      (
          Id   INTEGER PRIMARY KEY AUTOINCREMENT,
          Name TEXT NOT NULL
      )");

                Console.WriteLine("    OK");

                // ============================================================
                // [7-1] Commit 테스트
                // ============================================================

                Console.WriteLine();
                Console.WriteLine("    [7-1] Commit 테스트");

                DatabaseTransaction transaction =
                    context.BeginTransaction();

                try
                {
                    context.ExecuteNonQuery(
                        @"INSERT INTO TestTransaction (Name)
          VALUES ('Commit 테스트')",
                        null,
                        transaction);

                    transaction.Commit();

                    Console.WriteLine(
                        "        Commit OK");
                }
                finally
                {
                    transaction.Dispose();
                }

                object commitCount =
                    context.ExecuteScalar(
                        @"SELECT COUNT(*)
          FROM TestTransaction");

                int commitResult =
                    Convert.ToInt32(commitCount);

                if (commitResult != 1)
                {
                    throw new Exception(
                        "Commit 테스트 결과가 예상과 다릅니다. " +
                        "예상: 1, 실제: " + commitResult);
                }

                Console.WriteLine(
                    "        Commit 결과 확인 : " + commitResult);

                // ============================================================
                // [7-2] Rollback 테스트
                // ============================================================

                Console.WriteLine();
                Console.WriteLine("    [7-2] Rollback 테스트");

                transaction =
                    context.BeginTransaction();

                try
                {
                    context.ExecuteNonQuery(
                        @"INSERT INTO TestTransaction (Name)
          VALUES ('Rollback 테스트')",
                        null,
                        transaction);

                    transaction.Rollback();

                    Console.WriteLine(
                        "        Rollback OK");
                }
                finally
                {
                    transaction.Dispose();
                }

                object rollbackCount =
                    context.ExecuteScalar(
                        @"SELECT COUNT(*)
          FROM TestTransaction");

                int rollbackResult =
                    Convert.ToInt32(rollbackCount);

                if (rollbackResult != 1)
                {
                    throw new Exception(
                        "Rollback 테스트 결과가 예상과 다릅니다. " +
                        "예상: 1, 실제: " + rollbackResult);
                }

                Console.WriteLine(
                    "        Rollback 결과 확인 : " + rollbackResult);

                // ============================================================
                // [7-3] Dispose 자동 Rollback 테스트
                // ============================================================

                Console.WriteLine();
                Console.WriteLine("    [7-3] Dispose 자동 Rollback 테스트");

                transaction =
                    context.BeginTransaction();

                try
                {
                    context.ExecuteNonQuery(
                        @"INSERT INTO TestTransaction (Name)
          VALUES ('Dispose 테스트')",
                        null,
                        transaction);

                    Console.WriteLine(
                        "        Commit/Rollback 없이 Dispose");
                }
                finally
                {
                    // DatabaseTransaction.Dispose()는
                    // 완료되지 않은 Transaction을 자동 Rollback합니다.
                    transaction.Dispose();
                }

                object disposeCount =
                    context.ExecuteScalar(
                        @"SELECT COUNT(*)
          FROM TestTransaction");

                int disposeResult =
                    Convert.ToInt32(disposeCount);

                if (disposeResult != 1)
                {
                    throw new Exception(
                        "Dispose 자동 Rollback 테스트 결과가 예상과 다릅니다. " +
                        "예상: 1, 실제: " + disposeResult);
                }

                Console.WriteLine(
                    "        Dispose 자동 Rollback 결과 확인 : "
                    + disposeResult);

                // ============================================================
                // [7-4] 최종 데이터 확인
                // ============================================================

                Console.WriteLine();
                Console.WriteLine("    최종 데이터 확인");

                using (IDataReader reader =
                    context.ExecuteReader(
                        @"SELECT Id, Name
          FROM TestTransaction
          ORDER BY Id"))
                {
                    int count = 0;

                    while (reader.Read())
                    {
                        int id =
                            Convert.ToInt32(reader["Id"]);

                        string name =
                            Convert.ToString(reader["Name"]);

                        Console.WriteLine(
                            "        Id={0}, Name={1}",
                            id,
                            name);

                        count++;
                    }

                    if (count != 1)
                    {
                        throw new Exception(
                            "최종 데이터 수가 예상과 다릅니다. " +
                            "예상: 1, 실제: " + count);
                    }

                    Console.WriteLine(
                        "        최종 데이터 수 : " + count);
                }

                // ============================================================
                // 테이블 삭제
                // ============================================================

                Console.WriteLine();
                Console.WriteLine("    TestTransaction 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE TestTransaction");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " + droppedCount);

                Console.WriteLine();
                Console.WriteLine("    DatabaseTransaction 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }



        #region Helper

        private static TestUser GetFirstUser(
            GenericRepository<TestUser> repository)
        {
            List<TestUser> users =
                repository.Query(
                    @"SELECT Id, Name, Age
                      FROM TestUser
                      ORDER BY Id
                      LIMIT 1");

            if (users == null ||
                users.Count == 0)
            {
                return null;
            }

            return users[0];
        }


        private static void TestTransactionException(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[8] Transaction 예외 발생 Rollback 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            try
            {
                Console.WriteLine();
                Console.WriteLine("    TestTransactionException 테이블 생성");

                string createSql =
                    "CREATE TABLE TestTransactionException (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "Name TEXT NOT NULL" +
                    ")";

                context.ExecuteNonQuery(createSql);

                Console.WriteLine("    OK");

                Console.WriteLine();
                Console.WriteLine("    [8-1] 트랜잭션 시작");

                DatabaseTransaction transaction =
                    context.BeginTransaction();

                try
                {
                    Console.WriteLine("        첫 번째 데이터 입력");

                    string insertSql1 =
                        "INSERT INTO TestTransactionException (Name) " +
                        "VALUES ('정상 데이터')";

                    context.ExecuteNonQuery(
                        insertSql1,
                        null,
                        transaction);

                    Console.WriteLine("        OK");

                    Console.WriteLine();
                    Console.WriteLine("        [8-2] 의도적인 예외 발생");

                    /*
                     * Name 컬럼은 NOT NULL이므로
                     * NULL을 입력하면 SQLiteException이 발생합니다.
                     */
                    string insertSql2 =
                        "INSERT INTO TestTransactionException (Name) " +
                        "VALUES (NULL)";

                    context.ExecuteNonQuery(
                        insertSql2,
                        null,
                        transaction);

                    Console.WriteLine(
                        "        ERROR - 예외가 발생하지 않았습니다.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "        예외 발생 : " + ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " + ex.Message);

                    Console.WriteLine();
                    Console.WriteLine(
                        "        트랜잭션 Dispose");

                    /*
                     * Commit / Rollback을 호출하지 않고 Dispose합니다.
                     * DatabaseTransaction.Dispose()가 자동 Rollback을
                     * 수행하는지 확인합니다.
                     */
                    transaction.Dispose();

                    Console.WriteLine("        OK");
                }

                Console.WriteLine();
                Console.WriteLine("    [8-3] Rollback 결과 확인");

                string countSql =
                    "SELECT COUNT(*) " +
                    "FROM TestTransactionException";

                object countResult =
                    context.ExecuteScalar(
                        countSql,
                        null,
                        null);

                int count =
                    Convert.ToInt32(countResult);

                Console.WriteLine(
                    "        현재 데이터 수 : " + count);

                if (count != 0)
                {
                    throw new Exception(
                        "Rollback 실패 - 데이터가 남아 있습니다.");
                }

                Console.WriteLine(
                    "        OK - 모든 데이터가 Rollback 되었습니다.");

                Console.WriteLine();
                Console.WriteLine("    [8-4] 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE TestTransactionException");

                Console.WriteLine(
                    "        OK - 영향받은 객체 수 : " +
                    droppedCount);

                Console.WriteLine();
                Console.WriteLine(
                    "    Transaction 예외 Rollback 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }

        private static void TestGenericRepositoryTransaction(
            string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[9] GenericRepository Transaction 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            GenericRepository<TestUser> repository =
                new GenericRepository<TestUser>(context);

            try
            {
                Console.WriteLine();
                Console.WriteLine("    TestUser 테이블 준비");

                string createSql =
                    "CREATE TABLE TestUser (" +
                    "Id INTEGER PRIMARY KEY, " +
                    "Name TEXT NOT NULL, " +
                    "Age INTEGER NOT NULL" +
                    ")";

                context.ExecuteNonQuery(createSql);

                Console.WriteLine("    OK");

                // ------------------------------------------------------------
                // [9-1] Repository Execute + Transaction Commit
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    [9-1] Repository Execute + Commit");

                DatabaseTransaction transaction1 =
                    context.BeginTransaction();

                try
                {
                    repository.Execute(
                        "INSERT INTO TestUser (Id, Name, Age) " +
                        "VALUES (1, 'Commit 사용자', 30)",
                        null,
                        transaction1);

                    transaction1.Commit();

                    Console.WriteLine("        Commit OK");
                }
                finally
                {
                    transaction1.Dispose();
                }

                long count1 =
                    repository.Count();

                Console.WriteLine(
                    "        현재 Entity 수 : " + count1);

                if (count1 != 1)
                {
                    throw new Exception(
                        "Commit 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [9-2] Repository Execute + Transaction Rollback
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    [9-2] Repository Execute + Rollback");

                DatabaseTransaction transaction2 =
                    context.BeginTransaction();

                try
                {
                    repository.Execute(
                        "INSERT INTO TestUser (Id, Name, Age) " +
                        "VALUES (2, 'Rollback 사용자', 40)",
                        null,
                        transaction2);

                    transaction2.Rollback();

                    Console.WriteLine("        Rollback OK");
                }
                finally
                {
                    transaction2.Dispose();
                }

                long count2 =
                    repository.Count();

                Console.WriteLine(
                    "        현재 Entity 수 : " + count2);

                if (count2 != 1)
                {
                    throw new Exception(
                        "Rollback 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [9-3] Repository Query + Transaction
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    [9-3] Repository Query + Transaction");

                DatabaseTransaction transaction3 =
                    context.BeginTransaction();

                try
                {
                    repository.Execute(
                        "INSERT INTO TestUser (Id, Name, Age) " +
                        "VALUES (3, 'Transaction 사용자', 50)",
                        null,
                        transaction3);

                    List<TestUser> users =
                        repository.Query(
                            "SELECT Id, Name, Age " +
                            "FROM TestUser " +
                            "ORDER BY Id",
                            null,
                            transaction3);

                    Console.WriteLine(
                        "        Transaction 내부 조회 수 : " +
                        users.Count);

                    if (users.Count != 2)
                    {
                        throw new Exception(
                            "Transaction 내부 Query 결과가 올바르지 않습니다.");
                    }

                    TestUser lastUser =
                        users[users.Count - 1];

                    Console.WriteLine(
                        "        Id=" + lastUser.Id +
                        ", Name=" + lastUser.Name +
                        ", Age=" + lastUser.Age);

                    transaction3.Rollback();

                    Console.WriteLine(
                        "        Rollback OK");
                }
                finally
                {
                    transaction3.Dispose();
                }

                long count3 =
                    repository.Count();

                Console.WriteLine(
                    "        Rollback 후 Entity 수 : " +
                    count3);

                if (count3 != 1)
                {
                    throw new Exception(
                        "Query Transaction Rollback 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [9-4] Repository Scalar + Transaction
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    [9-4] Repository Scalar + Transaction");

                DatabaseTransaction transaction4 =
                    context.BeginTransaction();

                try
                {
                    repository.Execute(
                        "INSERT INTO TestUser (Id, Name, Age) " +
                        "VALUES (4, 'Scalar 사용자', 60)",
                        null,
                        transaction4);

                    object scalarResult =
                        repository.Scalar(
                            "SELECT COUNT(*) FROM TestUser",
                            null,
                            transaction4);

                    long transactionCount =
                        Convert.ToInt64(scalarResult);

                    Console.WriteLine(
                        "        Transaction 내부 Entity 수 : " +
                        transactionCount);

                    if (transactionCount != 2)
                    {
                        throw new Exception(
                            "Scalar Transaction 결과가 올바르지 않습니다.");
                    }

                    transaction4.Rollback();

                    Console.WriteLine(
                        "        Rollback OK");
                }
                finally
                {
                    transaction4.Dispose();
                }

                long count4 =
                    repository.Count();

                Console.WriteLine(
                    "        Rollback 후 Entity 수 : " +
                    count4);

                if (count4 != 1)
                {
                    throw new Exception(
                        "Scalar Transaction Rollback 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [9-5] InsertRange 내부 Transaction
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    [9-5] InsertRange 내부 Transaction");

                List<TestUser> insertUsers =
                    new List<TestUser>
                    {
                new TestUser
                {
                    Id = 10,
                    Name = "Range 사용자 1",
                    Age = 20
                },
                new TestUser
                {
                    Id = 11,
                    Name = "Range 사용자 2",
                    Age = 21
                },
                new TestUser
                {
                    Id = 12,
                    Name = "Range 사용자 3",
                    Age = 22
                }
                    };

                int insertedCount =
                    repository.InsertRange(insertUsers);

                Console.WriteLine(
                    "        InsertRange affected rows : " +
                    insertedCount);

                if (insertedCount != 3)
                {
                    throw new Exception(
                        "InsertRange 결과가 올바르지 않습니다.");
                }

                long count5 =
                    repository.Count();

                Console.WriteLine(
                    "        현재 Entity 수 : " +
                    count5);

                if (count5 != 4)
                {
                    throw new Exception(
                        "InsertRange 이후 Entity 수가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [9-6] InsertRange 예외 → 자동 Rollback
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [9-6] InsertRange 예외 + 자동 Rollback");

                List<TestUser> invalidUsers =
                    new List<TestUser>
                    {
                new TestUser
                {
                    Id = 20,
                    Name = "정상 Range 사용자",
                    Age = 30
                },

                null,

                new TestUser
                {
                    Id = 21,
                    Name = "두 번째 Range 사용자",
                    Age = 31
                }
                    };

                try
                {
                    repository.InsertRange(invalidUsers);

                    Console.WriteLine(
                        "        ERROR - 예외가 발생하지 않았습니다.");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(
                        "        예외 발생 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);

                    Console.WriteLine(
                        "        InsertRange 자동 Rollback 확인");
                }

                long count6 =
                    repository.Count();

                Console.WriteLine(
                    "        현재 Entity 수 : " +
                    count6);

                /*
                 * InsertRange 내부에서
                 *
                 * 20번 Entity INSERT
                 *     ↓
                 * null Entity 발견
                 *     ↓
                 * ArgumentException
                 *     ↓
                 * Commit 전에 Dispose
                 *     ↓
                 * 자동 Rollback
                 *
                 * 이 되어야 합니다.
                 */
                if (count6 != 4)
                {
                    throw new Exception(
                        "InsertRange 예외 발생 후 Rollback에 실패했습니다.");
                }

                Console.WriteLine(
                    "        OK - InsertRange 전체 Rollback");

                // ------------------------------------------------------------
                // 최종 데이터 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine("    최종 데이터 확인");

                List<TestUser> finalUsers =
                    repository.Query(
                        "SELECT Id, Name, Age " +
                        "FROM TestUser " +
                        "ORDER BY Id");

                foreach (TestUser user in finalUsers)
                {
                    Console.WriteLine(
                        "        Id=" + user.Id +
                        ", Name=" + user.Name +
                        ", Age=" + user.Age);
                }

                Console.WriteLine();
                Console.WriteLine("    TestUser 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE TestUser");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " +
                    droppedCount);

                Console.WriteLine();
                Console.WriteLine(
                    "    GenericRepository Transaction 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }

        private static void TestOrmMapping(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[10] ORM Mapping 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    factory,
                    dialect);

            GenericRepository<OrmMappingTestEntity> repository =
                new GenericRepository<OrmMappingTestEntity>(context);

            try
            {
                Console.WriteLine();
                Console.WriteLine("    OrmMappingTest 테이블 생성");

                string createSql =
                    "CREATE TABLE OrmMappingTest (" +
                    "id INTEGER PRIMARY KEY, " +
                    "display_name TEXT NOT NULL, " +
                    "age INTEGER NOT NULL, " +
                    "nullable_age INTEGER NULL, " +
                    "active INTEGER NOT NULL, " +
                    "status INTEGER NOT NULL, " +
                    "created_date TEXT NOT NULL, " +
                    "date_only TEXT NOT NULL, " +
                    "nullable_date TEXT NULL, " +
                    "guid_value TEXT NOT NULL, " +
                    "initial TEXT NOT NULL, " +
                    "server_value TEXT NULL" +
                    ")";

                context.ExecuteNonQuery(createSql);

                Console.WriteLine("    OK");

                // ------------------------------------------------------------
                // [10-1] TableAttribute / ColumnAttribute 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-1] TableAttribute / ColumnAttribute");

                OrmMappingTestEntity entity =
                    new OrmMappingTestEntity
                    {
                        Id = 1,
                        Name = "ORM 테스트",
                        Age = 50,
                        NullableAge = 35,
                        Active = true,
                        Status = TestUserStatus.Normal,
                        CreatedDate = new DateTime(
                            2026,
                            9,
                            22,
                            14,
                            30,
                            45),
                        DateOnly = new DateTime(
                            2026,
                            9,
                            22,
                            23,
                            59,
                            59),
                        NullableDate = new DateTime(
                            2026,
                            9,
                            23,
                            10,
                            20,
                            30),
                        GuidValue = Guid.NewGuid(),
                        Initial = 'K',
                        ServerValue = "DB 기본값"
                    };

                int inserted =
                    repository.Insert(entity);

                Console.WriteLine(
                    "        Insert affected rows : " +
                    inserted);

                if (inserted != 1)
                {
                    throw new Exception(
                        "ORM Mapping INSERT 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [10-2] GetById + 기본 타입 매핑
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-2] 기본 타입 / Attribute 매핑");

                
                Console.WriteLine();
                Console.WriteLine(
                    "        Guid DB 저장값 확인");

                object rawGuid =
                    context.ExecuteScalar(
                        "SELECT guid_value " +
                        "FROM OrmMappingTest " +
                        "WHERE id = 1");

                Console.WriteLine(
                    "        DB 값 : " +
                    (rawGuid == null ||
                     rawGuid == DBNull.Value
                        ? "NULL"
                        : rawGuid.ToString()));

                Console.WriteLine(
                    "        DB 타입 : " +
                    (rawGuid == null ||
                     rawGuid == DBNull.Value
                        ? "NULL"
                        : rawGuid.GetType().FullName));

                Console.WriteLine(
                    "        원본 Guid : " +
                    entity.GuidValue.ToString());

                Console.WriteLine(
                    "        원본 Guid 길이 : " +
                    entity.GuidValue.ToString().Length);

                
                Console.WriteLine();
                Console.WriteLine("        char DB 저장값 확인");

                object rawInitial =
                    context.ExecuteScalar(
                        "SELECT initial FROM OrmMappingTest WHERE id = 1");

                Console.WriteLine(
                    "        DB 값 : " +
                    (rawInitial == null || rawInitial == DBNull.Value
                        ? "NULL"
                        : rawInitial.ToString()));

                Console.WriteLine(
                    "        DB 타입 : " +
                    (rawInitial == null || rawInitial == DBNull.Value
                        ? "NULL"
                        : rawInitial.GetType().FullName));

                Console.WriteLine(
                    "        원본 char : " +
                    entity.Initial);

                Console.WriteLine(
                    "        원본 char 코드 : " +
                    ((int)entity.Initial));


                OrmMappingTestEntity loaded =
                    repository.GetById(1);

                if (loaded == null)
                {
                    throw new Exception(
                        "GetById 결과가 null입니다.");
                }

                Console.WriteLine(
                    "        Id=" + loaded.Id);

                Console.WriteLine(
                    "        Name=" + loaded.Name);

                Console.WriteLine(
                    "        Age=" + loaded.Age);

                Console.WriteLine(
                    "        NullableAge=" +
                    loaded.NullableAge);

                Console.WriteLine(
                    "        Active=" + loaded.Active);

                Console.WriteLine(
                    "        Status=" + loaded.Status);

                Console.WriteLine(
                    "        CreatedDate=" +
                    loaded.CreatedDate);

                Console.WriteLine(
                    "        DateOnly=" +
                    loaded.DateOnly);

                Console.WriteLine(
                    "        NullableDate=" +
                    loaded.NullableDate);

                Console.WriteLine(
                    "        GuidValue=" +
                    loaded.GuidValue);

                Console.WriteLine(
                    "        Initial=" +
                    loaded.Initial);

                Console.WriteLine(
                    "        ServerValue=" +
                    loaded.ServerValue);

                if (loaded.Id != 1)
                {
                    throw new Exception("Id 매핑 실패");
                }

                if (loaded.Name != "ORM 테스트")
                {
                    throw new Exception("ColumnAttribute Name 매핑 실패");
                }

                if (loaded.Age != 50)
                {
                    throw new Exception("int 매핑 실패");
                }

                if (!loaded.NullableAge.HasValue ||
                    loaded.NullableAge.Value != 35)
                {
                    throw new Exception("Nullable<int> 매핑 실패");
                }

                if (!loaded.Active)
                {
                    throw new Exception("bool 매핑 실패");
                }

                if (loaded.Status != TestUserStatus.Normal)
                {
                    throw new Exception("enum 매핑 실패");
                }

                if (loaded.CreatedDate != entity.CreatedDate)
                {
                    throw new Exception("DateTime 매핑 실패");
                }

                if (loaded.DateOnly.Date !=
                    entity.DateOnly.Date)
                {
                    throw new Exception(
                        "StoreDateOnly 매핑 결과가 올바르지 않습니다.");
                }

                if (!loaded.NullableDate.HasValue)
                {
                    throw new Exception(
                        "Nullable<DateTime> 매핑 실패");
                }

                if (loaded.NullableDate.Value !=
                    entity.NullableDate.Value)
                {
                    throw new Exception(
                        "Nullable<DateTime> 값이 올바르지 않습니다.");
                }

                if (loaded.GuidValue != entity.GuidValue)
                {
                    throw new Exception("Guid 매핑 실패");
                }

                if (loaded.Initial != 'K')
                {
                    throw new Exception("char 매핑 실패");
                }

                /*
                 * server_value는 INSERT 대상에서 제외했으므로
                 * 현재 DB 값은 NULL입니다.
                 */
                if (loaded.ServerValue != null)
                {
                    throw new Exception(
                        "IsInsertable=false 컬럼이 INSERT 되었습니다.");
                }

                Console.WriteLine(
                    "        기본 타입 및 Attribute 매핑 OK");

                // ------------------------------------------------------------
                // [10-3] StoreDateOnly 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-3] StoreDateOnly 테스트");

                /*
                 * 입력값:
                 *
                 * 2026-09-22 23:59:59
                 *
                 * StoreDateOnly=true이므로 DB에는
                 *
                 * 2026-09-22
                 *
                 * 형태로 저장되어야 합니다.
                 */

                OrmMappingTestEntity dateEntity =
                    new OrmMappingTestEntity
                    {
                        Id = 2,
                        Name = "DateOnly 테스트",
                        Age = 40,
                        NullableAge = null,
                        Active = false,
                        Status = TestUserStatus.Suspended,
                        CreatedDate = new DateTime(
                            2026,
                            9,
                            22,
                            8,
                            10,
                            20),
                        DateOnly = new DateTime(
                            2026,
                            9,
                            22,
                            23,
                            59,
                            59),
                        NullableDate = null,
                        GuidValue = Guid.NewGuid(),
                        Initial = 'D'
                    };

                repository.Insert(dateEntity);

                OrmMappingTestEntity loadedDate =
                    repository.GetById(2);

                Console.WriteLine(
                    "        입력 DateOnly : " +
                    dateEntity.DateOnly);

                Console.WriteLine(
                    "        조회 DateOnly : " +
                    loadedDate.DateOnly);

                if (loadedDate.DateOnly !=
                    dateEntity.DateOnly.Date)
                {
                    throw new Exception(
                        "StoreDateOnly가 정상적으로 처리되지 않았습니다.");
                }

                Console.WriteLine(
                    "        StoreDateOnly OK");

                // ------------------------------------------------------------
                // [10-4] Nullable / DB NULL 테스트
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-4] Nullable / DB NULL 매핑");

                OrmMappingTestEntity nullEntity =
                    new OrmMappingTestEntity
                    {
                        Id = 3,
                        Name = "NULL 테스트",
                        Age = 30,
                        NullableAge = null,
                        Active = false,
                        Status = TestUserStatus.Deleted,
                        CreatedDate = new DateTime(
                            2026,
                            9,
                            24,
                            12,
                            0,
                            0),
                        DateOnly = new DateTime(
                            2026,
                            9,
                            24),
                        NullableDate = null,
                        GuidValue = Guid.NewGuid(),
                        Initial = 'N'
                    };

                repository.Insert(nullEntity);

                OrmMappingTestEntity loadedNull =
                    repository.GetById(3);

                Console.WriteLine(
                    "        NullableAge : " +
                    (loadedNull.NullableAge.HasValue
                        ? loadedNull.NullableAge.Value.ToString()
                        : "NULL"));

                Console.WriteLine(
                    "        NullableDate : " +
                    (loadedNull.NullableDate.HasValue
                        ? loadedNull.NullableDate.Value.ToString()
                        : "NULL"));

                if (loadedNull.NullableAge.HasValue)
                {
                    throw new Exception(
                        "DB NULL → Nullable<int> 매핑 실패");
                }

                if (loadedNull.NullableDate.HasValue)
                {
                    throw new Exception(
                        "DB NULL → Nullable<DateTime> 매핑 실패");
                }

                Console.WriteLine(
                    "        Nullable / DB NULL 매핑 OK");

                // ------------------------------------------------------------
                // [10-5] enum / bool / Guid 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-5] enum / bool / Guid 매핑");

                if (loadedNull.Status !=
                    TestUserStatus.Deleted)
                {
                    throw new Exception(
                        "enum 매핑 실패");
                }

                if (loadedNull.Active)
                {
                    throw new Exception(
                        "bool 매핑 실패");
                }

                if (loadedNull.GuidValue == Guid.Empty)
                {
                    throw new Exception(
                        "Guid 매핑 실패");
                }

                Console.WriteLine(
                    "        enum=" +
                    loadedNull.Status);

                Console.WriteLine(
                    "        bool=" +
                    loadedNull.Active);

                Console.WriteLine(
                    "        Guid=" +
                    loadedNull.GuidValue);

                Console.WriteLine(
                    "        OK");

                // ------------------------------------------------------------
                // [10-6] IsInsertable=false 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-6] IsInsertable=false 테스트");

                string serverValueSql =
                    "SELECT server_value " +
                    "FROM OrmMappingTest " +
                    "WHERE id = 1";

                object serverValue =
                    context.ExecuteScalar(
                        serverValueSql);

                Console.WriteLine(
                    "        DB server_value : " +
                    (serverValue == null ||
                     serverValue == DBNull.Value
                        ? "NULL"
                        : serverValue.ToString()));

                if (serverValue != null &&
                    serverValue != DBNull.Value)
                {
                    throw new Exception(
                        "IsInsertable=false 컬럼이 INSERT 되었습니다.");
                }

                Console.WriteLine(
                    "        OK");

                // ------------------------------------------------------------
                // [10-7] IsUpdatable=false 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-7] IsUpdatable=false 테스트");

                loaded =
                    repository.GetById(1);

                loaded.Name =
                    "ORM 테스트 수정";

                loaded.ServerValue =
                    "수정되어서는 안 됨";

                /*
                 * Update에서는 ServerValue가
                 * IsUpdatable=false이므로 UPDATE 대상에서
                 * 제외되어야 합니다.
                 */
                int updated =
                    repository.Update(loaded);

                Console.WriteLine(
                    "        Update affected rows : " +
                    updated);

                OrmMappingTestEntity updatedEntity =
                    repository.GetById(1);

                if (updatedEntity.Name !=
                    "ORM 테스트 수정")
                {
                    throw new Exception(
                        "일반 Column Update 실패");
                }

                if (updatedEntity.ServerValue != null)
                {
                    throw new Exception(
                        "IsUpdatable=false 컬럼이 UPDATE 되었습니다.");
                }

                Console.WriteLine(
                    "        Name Update OK");

                Console.WriteLine(
                    "        ServerValue Update 제외 OK");

                // ------------------------------------------------------------
                // [10-8] 전체 Query / MapList 테스트
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [10-8] 전체 Entity Query / MapList");

                List<OrmMappingTestEntity> entities =
                    repository.Query(
                        "SELECT " +
                        "id, " +
                        "display_name, " +
                        "age, " +
                        "nullable_age, " +
                        "active, " +
                        "status, " +
                        "created_date, " +
                        "date_only, " +
                        "nullable_date, " +
                        "guid_value, " +
                        "initial, " +
                        "server_value " +
                        "FROM OrmMappingTest " +
                        "ORDER BY id");

                Console.WriteLine(
                    "        조회된 Entity 수 : " +
                    entities.Count);

                if (entities.Count != 3)
                {
                    throw new Exception(
                        "MapList 결과 개수가 올바르지 않습니다.");
                }

                foreach (OrmMappingTestEntity item in entities)
                {
                    Console.WriteLine(
                        "        Id=" + item.Id +
                        ", Name=" + item.Name +
                        ", Age=" + item.Age +
                        ", Status=" + item.Status);
                }

                Console.WriteLine(
                    "        OK");

                // ------------------------------------------------------------
                // 최종 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    OrmMappingTest 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE OrmMappingTest");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " +
                    droppedCount);

                Console.WriteLine();
                Console.WriteLine(
                    "    ORM Mapping 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }

        
        private static void TestSqlBuilder(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[11] SQL Builder 테스트");
            Console.WriteLine();

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(factory, dialect);

            try
            {
                Console.WriteLine(
                    "    SqlBuilderTest 테이블 생성");

                context.ExecuteNonQuery(
                    "DROP TABLE IF EXISTS SqlBuilderTest;");

                context.ExecuteNonQuery(
                    "CREATE TABLE SqlBuilderTest (" +
                    "id INTEGER PRIMARY KEY, " +
                    "name TEXT NOT NULL, " +
                    "age INTEGER NOT NULL, " +
                    "server_value TEXT NULL" +
                    ");");

                Console.WriteLine("    OK");

                EntityMetadata metadata =
                    EntityMetadataCache.GetMetadata<SqlBuilderTestEntity>();

                Console.WriteLine();

                // ------------------------------------------------------------
                // [11-1] InsertSqlBuilder
                // ------------------------------------------------------------

                Console.WriteLine(
                    "    [11-1] InsertSqlBuilder");

                string insertSql =
                    InsertSqlBuilder.Build(
                        metadata,
                        dialect);

                Console.WriteLine(
                    "        SQL : " + insertSql);

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = insertSql;

                    AddParameter(
                        command,
                        "@id",
                        1);

                    AddParameter(
                        command,
                        "@name",
                        "Insert 테스트");

                    AddParameter(
                        command,
                        "@age",
                        30);

                    int affected =
                        command.ExecuteNonQuery();

                    Console.WriteLine(
                        "        Insert affected rows : " +
                        affected);

                    if (affected != 1)
                    {
                        throw new Exception(
                            "InsertSqlBuilder 실행 결과가 올바르지 않습니다.");
                    }
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-2] SelectSqlBuilder
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-2] SelectSqlBuilder");

                string selectSql =
                    SelectSqlBuilder.Build(
                        metadata,
                        dialect,
                        "id = @id");

                Console.WriteLine(
                    "        SQL : " + selectSql);

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = selectSql;

                    AddParameter(
                        command,
                        "@id",
                        1);

                    using (IDataReader reader =
                           command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new Exception(
                                "SelectSqlBuilder 조회 결과가 없습니다.");
                        }

                        int id =
                            Convert.ToInt32(
                                reader["id"]);

                        string name =
                            Convert.ToString(
                                reader["name"]);

                        int age =
                            Convert.ToInt32(
                                reader["age"]);

                        Console.WriteLine(
                            "        Id=" + id +
                            ", Name=" + name +
                            ", Age=" + age);

                        if (id != 1 ||
                            name != "Insert 테스트" ||
                            age != 30)
                        {
                            throw new Exception(
                                "SelectSqlBuilder 조회 결과가 올바르지 않습니다.");
                        }
                    }
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-3] UpdateSqlBuilder
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-3] UpdateSqlBuilder");

                string updateSql =
                    UpdateSqlBuilder.Build(
                        metadata,
                        dialect);

                Console.WriteLine(
                    "        SQL : " + updateSql);

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = updateSql;

                    AddParameter(
                        command,
                        "@name",
                        "Update 테스트");

                    AddParameter(
                        command,
                        "@age",
                        40);

                    AddParameter(
                        command,
                        "@id",
                        1);

                    int affected =
                        command.ExecuteNonQuery();

                    Console.WriteLine(
                        "        Update affected rows : " +
                        affected);

                    if (affected != 1)
                    {
                        throw new Exception(
                            "UpdateSqlBuilder 실행 결과가 올바르지 않습니다.");
                    }
                }

                object updatedName =
                    context.ExecuteScalar(
                        "SELECT name FROM SqlBuilderTest WHERE id = 1;");

                object updatedAge =
                    context.ExecuteScalar(
                        "SELECT age FROM SqlBuilderTest WHERE id = 1;");

                Console.WriteLine(
                    "        Name : " +
                    Convert.ToString(updatedName));

                Console.WriteLine(
                    "        Age  : " +
                    Convert.ToString(updatedAge));

                if (Convert.ToString(updatedName) !=
                        "Update 테스트")
                {
                    throw new Exception(
                        "UpdateSqlBuilder Name 변경에 실패했습니다.");
                }

                if (Convert.ToInt32(updatedAge) != 40)
                {
                    throw new Exception(
                        "UpdateSqlBuilder Age 변경에 실패했습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-4] UpsertSqlBuilder - INSERT
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-4] UpsertSqlBuilder - INSERT");

                string upsertSql =
                    UpsertSqlBuilder.Build(
                        metadata,
                        dialect);

                Console.WriteLine(
                    "        SQL : " + upsertSql);

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = upsertSql;

                    AddParameter(
                        command,
                        "@id",
                        2);

                    AddParameter(
                        command,
                        "@name",
                        "Upsert INSERT");

                    AddParameter(
                        command,
                        "@age",
                        50);

                    int affected =
                        command.ExecuteNonQuery();

                    Console.WriteLine(
                        "        Upsert affected rows : " +
                        affected);

                    if (affected != 1)
                    {
                        throw new Exception(
                            "UpsertSqlBuilder INSERT 실행에 실패했습니다.");
                    }
                }

                object upsertInsertName =
                    context.ExecuteScalar(
                        "SELECT name FROM SqlBuilderTest WHERE id = 2;");

                object upsertInsertAge =
                    context.ExecuteScalar(
                        "SELECT age FROM SqlBuilderTest WHERE id = 2;");

                Console.WriteLine(
                    "        Name : " +
                    Convert.ToString(upsertInsertName));

                Console.WriteLine(
                    "        Age  : " +
                    Convert.ToString(upsertInsertAge));

                if (Convert.ToString(upsertInsertName) !=
                        "Upsert INSERT" ||
                    Convert.ToInt32(upsertInsertAge) != 50)
                {
                    throw new Exception(
                        "Upsert INSERT 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-5] UpsertSqlBuilder - UPDATE
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-5] UpsertSqlBuilder - UPDATE");

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = upsertSql;

                    AddParameter(
                        command,
                        "@id",
                        2);

                    AddParameter(
                        command,
                        "@name",
                        "Upsert UPDATE");

                    AddParameter(
                        command,
                        "@age",
                        60);

                    int affected =
                        command.ExecuteNonQuery();

                    Console.WriteLine(
                        "        Upsert affected rows : " +
                        affected);

                    if (affected != 1)
                    {
                        throw new Exception(
                            "UpsertSqlBuilder UPDATE 실행에 실패했습니다.");
                    }
                }

                object upsertUpdateName =
                    context.ExecuteScalar(
                        "SELECT name FROM SqlBuilderTest WHERE id = 2;");

                object upsertUpdateAge =
                    context.ExecuteScalar(
                        "SELECT age FROM SqlBuilderTest WHERE id = 2;");

                Console.WriteLine(
                    "        Name : " +
                    Convert.ToString(upsertUpdateName));

                Console.WriteLine(
                    "        Age  : " +
                    Convert.ToString(upsertUpdateAge));

                if (Convert.ToString(upsertUpdateName) !=
                        "Upsert UPDATE" ||
                    Convert.ToInt32(upsertUpdateAge) != 60)
                {
                    throw new Exception(
                        "Upsert UPDATE 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-6] DeleteSqlBuilder
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-6] DeleteSqlBuilder");

                string deleteSql =
                    DeleteSqlBuilder.Build(
                        metadata,
                        dialect);

                Console.WriteLine(
                    "        SQL : " + deleteSql);

                using (IDbConnection connection =
                       context.OpenConnection())
                using (IDbCommand command =
                       connection.CreateCommand())
                {
                    command.CommandText = deleteSql;

                    AddParameter(
                        command,
                        "@id",
                        2);

                    int affected =
                        command.ExecuteNonQuery();

                    Console.WriteLine(
                        "        Delete affected rows : " +
                        affected);

                    if (affected != 1)
                    {
                        throw new Exception(
                            "DeleteSqlBuilder 실행 결과가 올바르지 않습니다.");
                    }
                }

                object deletedCount =
                    context.ExecuteScalar(
                        "SELECT COUNT(*) " +
                        "FROM SqlBuilderTest " +
                        "WHERE id = 2;");

                Console.WriteLine(
                    "        삭제 후 Id=2 count : " +
                    Convert.ToInt32(deletedCount));

                if (Convert.ToInt32(deletedCount) != 0)
                {
                    throw new Exception(
                        "DeleteSqlBuilder 삭제 결과가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // [11-7] Builder SQL 최종 검증
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [11-7] Builder SQL 구조 검증");

                if (insertSql.IndexOf(
                        "INSERT INTO",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "INSERT SQL 구조가 올바르지 않습니다.");
                }

                if (updateSql.IndexOf(
                        "UPDATE",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    updateSql.IndexOf(
                        "WHERE",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "UPDATE SQL 구조가 올바르지 않습니다.");
                }

                if (deleteSql.IndexOf(
                        "DELETE FROM",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    deleteSql.IndexOf(
                        "WHERE",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "DELETE SQL 구조가 올바르지 않습니다.");
                }

                if (selectSql.IndexOf(
                        "SELECT",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    selectSql.IndexOf(
                        "WHERE",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "SELECT SQL 구조가 올바르지 않습니다.");
                }

                if (upsertSql.IndexOf(
                        "ON CONFLICT",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "UPSERT SQL 구조가 올바르지 않습니다.");
                }

                Console.WriteLine("        OK");

                // ------------------------------------------------------------
                // 최종 데이터 확인
                // ------------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    최종 데이터 확인");

                object finalCount =
                    context.ExecuteScalar(
                        "SELECT COUNT(*) FROM SqlBuilderTest;");

                Console.WriteLine(
                    "        최종 Row 수 : " +
                    Convert.ToInt32(finalCount));

                if (Convert.ToInt32(finalCount) != 1)
                {
                    throw new Exception(
                        "최종 Row 수가 예상과 다릅니다.");
                }

                Console.WriteLine();
                Console.WriteLine(
                    "    SqlBuilderTest 테이블 삭제");

                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE SqlBuilderTest;");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " +
                    droppedCount);

                Console.WriteLine();
                Console.WriteLine(
                    "    SQL Builder 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }

     
        private static void TestGenericRepositoryConditions(
            string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[12] GenericRepository 조건 / Scalar 테스트");

            SqliteConnectionFactory factory =
                new SqliteConnectionFactory(connectionString);

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(factory, dialect);

            try
            {
                GenericRepository<TestUser> repository =
                    new GenericRepository<TestUser>(context);

                // ---------------------------------------------------------
                // [12-1] TestUser 테이블 생성
                // ---------------------------------------------------------
                context.ExecuteNonQuery(
                    "DROP TABLE IF EXISTS TestUser;");

                context.ExecuteNonQuery(
                    "CREATE TABLE TestUser (" +
                    "id INTEGER PRIMARY KEY, " +
                    "name TEXT NOT NULL, " +
                    "age INTEGER NOT NULL" +
                    ");");

                Console.WriteLine(
                    "    TestUser 테이블 생성 OK");

                // ---------------------------------------------------------
                // [12-2] 테스트 데이터 입력
                // ---------------------------------------------------------
                context.ExecuteNonQuery(
                    "INSERT INTO TestUser " +
                    "(id, name, age) VALUES " +
                    "(1, '홍길동', 20);");

                context.ExecuteNonQuery(
                    "INSERT INTO TestUser " +
                    "(id, name, age) VALUES " +
                    "(2, '김철수', 30);");

                context.ExecuteNonQuery(
                    "INSERT INTO TestUser " +
                    "(id, name, age) VALUES " +
                    "(3, '이영희', 40);");

                context.ExecuteNonQuery(
                    "INSERT INTO TestUser " +
                    "(id, name, age) VALUES " +
                    "(4, '박민수', 50);");

                Console.WriteLine(
                    "    테스트 데이터 4건 입력 OK");

                // ---------------------------------------------------------
                // [12-3] Count()
                // ---------------------------------------------------------
                long totalCount =
                    repository.Count();

                Console.WriteLine(
                    "    전체 Count : " + totalCount);

                if (totalCount != 4)
                {
                    throw new Exception(
                        "Count() 테스트 실패");
                }

                Console.WriteLine(
                    "    Count() OK");

                // ---------------------------------------------------------
                // [12-4] Count(whereClause)
                // ---------------------------------------------------------
                long ageCount =
                    repository.Count(
                        "age >= 30",
                        null);

                Console.WriteLine(
                    "    age >= 30 Count : " + ageCount);

                if (ageCount != 3)
                {
                    throw new Exception(
                        "조건 Count() 테스트 실패");
                }

                Console.WriteLine(
                    "    조건 Count() OK");

                // ---------------------------------------------------------
                // [12-5] Exists()
                // ---------------------------------------------------------
                bool exists =
                    repository.Exists(
                        "age >= 40",
                        null);

                Console.WriteLine(
                    "    age >= 40 Exists : " + exists);

                if (!exists)
                {
                    throw new Exception(
                        "Exists() true 테스트 실패");
                }

                bool notExists =
                    repository.Exists(
                        "age >= 100",
                        null);

                Console.WriteLine(
                    "    age >= 100 Exists : " + notExists);

                if (notExists)
                {
                    throw new Exception(
                        "Exists() false 테스트 실패");
                }

                Console.WriteLine(
                    "    Exists() OK");

                // ---------------------------------------------------------
                // [12-6] Query() + 조건
                // ---------------------------------------------------------
                List<TestUser> queryResult =
                    repository.Query(
                        "SELECT id, name, age " +
                        "FROM TestUser " +
                        "WHERE age >= 35;");

                Console.WriteLine(
                    "    Query 결과 수 : " + queryResult.Count);

                if (queryResult.Count != 2)
                {
                    throw new Exception(
                        "Query() 조건 테스트 실패");
                }

                foreach (TestUser user in queryResult)
                {
                    Console.WriteLine(
                        "    Id=" + user.Id +
                        ", Name=" + user.Name +
                        ", Age=" + user.Age);
                }

                Console.WriteLine(
                    "    Query() 조건 OK");

                // ---------------------------------------------------------
                // [12-7] Scalar()
                // ---------------------------------------------------------
                object scalarResult =
                    repository.Scalar(
                        "SELECT COUNT(*) FROM TestUser;");

                long scalarCount =
                    Convert.ToInt64(scalarResult);

                Console.WriteLine(
                    "    Scalar COUNT : " + scalarCount);

                if (scalarCount != 4)
                {
                    throw new Exception(
                        "Scalar() 테스트 실패");
                }

                Console.WriteLine(
                    "    Scalar() OK");

                // ---------------------------------------------------------
                // [12-8] DeleteWhere()
                // ---------------------------------------------------------
                int deletedRows =
                    repository.DeleteWhere(
                        "age >= 40",
                        null);

                Console.WriteLine(
                    "    DeleteWhere affected : " + deletedRows);

                if (deletedRows != 2)
                {
                    throw new Exception(
                        "DeleteWhere() 테스트 실패");
                }

                long remainingCount =
                    repository.Count();

                Console.WriteLine(
                    "    삭제 후 Count : " + remainingCount);

                if (remainingCount != 2)
                {
                    throw new Exception(
                        "DeleteWhere() 결과 Count 테스트 실패");
                }

                Console.WriteLine(
                    "    DeleteWhere() OK");

                // ---------------------------------------------------------
                // [12-9] WHERE 포함 조건
                //
                // 현재 데이터:
                //   Id=1 Age=20
                //   Id=2 Age=30
                //
                // 따라서 age >= 30 은 1건입니다.
                // ---------------------------------------------------------
                long whereCount =
                    repository.Count(
                        "WHERE age >= 30",
                        null);

                Console.WriteLine(
                    "    'WHERE age >= 30' Count : " +
                    whereCount);

                if (whereCount != 1)
                {
                    throw new Exception(
                        "WHERE 포함 조건 테스트 실패");
                }

                Console.WriteLine(
                    "    WHERE 조건 처리 OK");

                // ---------------------------------------------------------
                // [12-10] 잘못된 WHERE 조건 예외
                // ---------------------------------------------------------
                bool exceptionConfirmed = false;

                try
                {
                    repository.DeleteWhere(
                        "",
                        null);
                }
                catch (ArgumentException ex)
                {
                    exceptionConfirmed = true;

                    Console.WriteLine(
                        "    예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "    메시지 : " +
                        ex.Message);
                }

                if (!exceptionConfirmed)
                {
                    throw new Exception(
                        "잘못된 WHERE 조건 예외 테스트 실패");
                }

                Console.WriteLine(
                    "    잘못된 WHERE 조건 예외 처리 OK");

                // ---------------------------------------------------------
                // [12-11] 잘못된 Query SQL 예외
                // ---------------------------------------------------------
                exceptionConfirmed = false;

                try
                {
                    repository.Query("");
                }
                catch (ArgumentException ex)
                {
                    exceptionConfirmed = true;

                    Console.WriteLine(
                        "    예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "    메시지 : " +
                        ex.Message);
                }

                if (!exceptionConfirmed)
                {
                    throw new Exception(
                        "잘못된 Query SQL 예외 테스트 실패");
                }

                Console.WriteLine(
                    "    잘못된 Query SQL 예외 처리 OK");

                // ---------------------------------------------------------
                // [12-12] 최종 데이터 확인
                // ---------------------------------------------------------
                List<TestUser> finalUsers =
                    repository.Query(
                        "SELECT id, name, age " +
                        "FROM TestUser " +
                        "ORDER BY id;");

                Console.WriteLine(
                    "    최종 데이터 수 : " +
                    finalUsers.Count);

                foreach (TestUser user in finalUsers)
                {
                    Console.WriteLine(
                        "    Id=" + user.Id +
                        ", Name=" + user.Name +
                        ", Age=" + user.Age);
                }

                if (finalUsers.Count != 2)
                {
                    throw new Exception(
                        "최종 데이터 확인 실패");
                }

                // ---------------------------------------------------------
                // [12-13] 테이블 삭제
                // ---------------------------------------------------------
                int droppedCount =
                    context.ExecuteNonQuery(
                        "DROP TABLE IF EXISTS TestUser;");

                Console.WriteLine(
                    "    TestUser 삭제 - 영향받은 행 : " +
                    droppedCount);

                Console.WriteLine(
                    "    GenericRepository 조건 / Scalar 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }


 
        private static void TestParameterFactory(
            string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("[13] IDbParameterFactory 테스트");

            SqliteConnectionFactory connectionFactory =
                new SqliteConnectionFactory(connectionString);

            SqliteParameterFactory parameterFactory =
                new SqliteParameterFactory();

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    connectionFactory,
                    parameterFactory,
                    dialect);

            try
            {
                // ---------------------------------------------------------
                // 테스트 테이블 준비
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    ParameterFactoryTest 테이블 생성");

                context.ExecuteNonQuery(
                    "DROP TABLE IF EXISTS ParameterFactoryTest");

                context.ExecuteNonQuery(
                    "CREATE TABLE ParameterFactoryTest (" +
                    "id INTEGER PRIMARY KEY, " +
                    "name TEXT, " +
                    "age INTEGER)");

                Console.WriteLine("    OK");

                // ---------------------------------------------------------
                // [13-1] CreateParameter 기본 테스트
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-1] CreateParameter 기본 테스트");

                IDbDataParameter idParameter =
                    context.CreateParameter(
                        "@id",
                        1);

                IDbDataParameter nameParameter =
                    context.CreateParameter(
                        "@name",
                        "홍길동");

                IDbDataParameter ageParameter =
                    context.CreateParameter(
                        "@age",
                        30);

                if (idParameter == null)
                    throw new Exception(
                        "idParameter가 null입니다.");

                if (nameParameter == null)
                    throw new Exception(
                        "nameParameter가 null입니다.");

                if (ageParameter == null)
                    throw new Exception(
                        "ageParameter가 null입니다.");

                Console.WriteLine(
                    "        Parameter Type : " +
                    idParameter.GetType().FullName);

                Console.WriteLine(
                    "        id Name : " +
                    idParameter.ParameterName);

                Console.WriteLine(
                    "        id Value : " +
                    idParameter.Value);

                Console.WriteLine(
                    "        name Value : " +
                    nameParameter.Value);

                Console.WriteLine(
                    "        age Value : " +
                    ageParameter.Value);

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // [13-2] CreateParameter + ExecuteNonQuery
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-2] CreateParameter + ExecuteNonQuery");

                int affectedRows =
                    context.ExecuteNonQuery(
                        "INSERT INTO ParameterFactoryTest " +
                        "(id, name, age) " +
                        "VALUES (@id, @name, @age)",
                        new IDbDataParameter[]
                        {
                    idParameter,
                    nameParameter,
                    ageParameter
                        });

                Console.WriteLine(
                    "        Insert affected rows : " +
                    affectedRows);

                if (affectedRows != 1)
                {
                    throw new Exception(
                        "INSERT 결과가 예상과 다릅니다. " +
                        "예상: 1, 실제: " +
                        affectedRows);
                }

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // [13-3] CreateParameter + ExecuteScalar
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-3] CreateParameter + ExecuteScalar");

                IDbDataParameter scalarIdParameter =
                    context.CreateParameter(
                        "@id",
                        1);

                object scalarResult =
                    context.ExecuteScalar(
                        "SELECT age " +
                        "FROM ParameterFactoryTest " +
                        "WHERE id = @id",
                        new IDbDataParameter[]
                        {
                    scalarIdParameter
                        });

                Console.WriteLine(
                    "        Scalar 결과 : " +
                    scalarResult);

                int scalarAge =
                    Convert.ToInt32(scalarResult);

                if (scalarAge != 30)
                {
                    throw new Exception(
                        "Scalar 결과가 예상과 다릅니다. " +
                        "예상: 30, 실제: " +
                        scalarAge);
                }

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // [13-4] CreateParameter + ExecuteReader
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-4] CreateParameter + ExecuteReader");

                IDbDataParameter readerIdParameter =
                    context.CreateParameter(
                        "@id",
                        1);

                int readerCount = 0;

                using (IDataReader reader =
                       context.ExecuteReader(
                           "SELECT id, name, age " +
                           "FROM ParameterFactoryTest " +
                           "WHERE id = @id",
                           new IDbDataParameter[]
                           {
                       readerIdParameter
                           }))
                {
                    while (reader.Read())
                    {
                        readerCount++;

                        int id =
                            Convert.ToInt32(
                                reader["id"]);

                        string name =
                            Convert.ToString(
                                reader["name"]);

                        int age =
                            Convert.ToInt32(
                                reader["age"]);

                        Console.WriteLine(
                            "        Id={0}, Name={1}, Age={2}",
                            id,
                            name,
                            age);
                    }
                }

                if (readerCount != 1)
                {
                    throw new Exception(
                        "Reader 조회 결과가 예상과 다릅니다. " +
                        "예상: 1, 실제: " +
                        readerCount);
                }

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // [13-5] null 값 테스트
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-5] null 값 테스트");

                IDbDataParameter nullParameter =
                    context.CreateParameter(
                        "@value",
                        null);

                Console.WriteLine(
                    "        Parameter Value Type : " +
                    nullParameter.Value.GetType().FullName);

                Console.WriteLine(
                    "        Parameter Value : " +
                    nullParameter.Value);

                if (nullParameter.Value != DBNull.Value)
                {
                    throw new Exception(
                        "null 값이 DBNull.Value로 변환되지 않았습니다.");
                }

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // [13-6] 잘못된 Parameter 이름 예외 테스트
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    [13-6] 잘못된 Parameter 이름 예외 테스트");

                bool exceptionThrown = false;

                try
                {
                    context.CreateParameter(
                        "",
                        100);
                }
                catch (ArgumentException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "빈 Parameter 이름에 대한 " +
                        "예외가 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                // ---------------------------------------------------------
                // 테이블 삭제
                // ---------------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "    ParameterFactoryTest 테이블 삭제");

                int dropped =
                    context.ExecuteNonQuery(
                        "DROP TABLE ParameterFactoryTest");

                Console.WriteLine(
                    "    OK - 영향받은 객체 수 : " +
                    dropped);

                Console.WriteLine();
                Console.WriteLine(
                    "    IDbParameterFactory 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }


        
        private static void TestDatabaseOptions()
        {
            Console.WriteLine();
            Console.WriteLine("[14] DatabaseOptions 테스트");

            Console.WriteLine();
            Console.WriteLine(
                "    [14-1] 정상적인 DatabaseOptions 테스트");

            DatabaseOptions validOptions =
                new DatabaseOptions();

            validOptions.Provider =
                DatabaseProvider.SQLite;

            validOptions.ConnectionString =
                "Data Source=test.db;";

            validOptions.ConnectionTimeout = 30;

            validOptions.Validate();

            Console.WriteLine(
                "        Provider : " +
                validOptions.Provider);

            Console.WriteLine(
                "        ConnectionTimeout : " +
                validOptions.ConnectionTimeout);

            Console.WriteLine("        Validate() : OK");

            Console.WriteLine();
            Console.WriteLine(
                "    [14-2] ConnectionString 누락 예외 테스트");

            DatabaseOptions emptyConnectionStringOptions =
                new DatabaseOptions();

            emptyConnectionStringOptions.Provider =
                DatabaseProvider.SQLite;

            emptyConnectionStringOptions.ConnectionTimeout = 30;

            bool connectionStringExceptionThrown = false;

            try
            {
                emptyConnectionStringOptions.Validate();
            }
            catch (InvalidOperationException ex)
            {
                connectionStringExceptionThrown = true;

                Console.WriteLine(
                    "        예상된 예외 : " +
                    ex.GetType().Name);

                Console.WriteLine(
                    "        메시지 : " +
                    ex.Message);
            }

            if (!connectionStringExceptionThrown)
            {
                throw new Exception(
                    "ConnectionString 누락에 대한 " +
                    "예외가 발생하지 않았습니다.");
            }

            Console.WriteLine("        OK");

            Console.WriteLine();
            Console.WriteLine(
                "    [14-3] ConnectionTimeout 음수 예외 테스트");

            DatabaseOptions negativeTimeoutOptions =
                new DatabaseOptions();

            negativeTimeoutOptions.Provider =
                DatabaseProvider.SQLite;

            negativeTimeoutOptions.ConnectionString =
                "Data Source=test.db;";

            negativeTimeoutOptions.ConnectionTimeout = -1;

            bool timeoutExceptionThrown = false;

            try
            {
                negativeTimeoutOptions.Validate();
            }
            catch (InvalidOperationException ex)
            {
                timeoutExceptionThrown = true;

                Console.WriteLine(
                    "        예상된 예외 : " +
                    ex.GetType().Name);

                Console.WriteLine(
                    "        메시지 : " +
                    ex.Message);
            }

            if (!timeoutExceptionThrown)
            {
                throw new Exception(
                    "음수 ConnectionTimeout에 대한 " +
                    "예외가 발생하지 않았습니다.");
            }

            Console.WriteLine("        OK");

            Console.WriteLine();
            Console.WriteLine(
                "    DatabaseOptions 테스트 PASS");
        }



        private static void TestDatabaseContextDispose(
            string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine(
                "[15] DatabaseContext Dispose 테스트");

            Console.WriteLine();
            Console.WriteLine(
                "    [15-1] Dispose 후 CreateConnection() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.CreateConnection();
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 CreateConnection()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-2] Dispose 후 OpenConnection() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.OpenConnection();
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 OpenConnection()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-3] Dispose 후 CreateParameter() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.CreateParameter(
                        "@id",
                        1);
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 CreateParameter()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-4] Dispose 후 ExecuteNonQuery() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.ExecuteNonQuery(
                        "SELECT 1");
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 ExecuteNonQuery()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-5] Dispose 후 ExecuteScalar() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.ExecuteScalar(
                        "SELECT 1");
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 ExecuteScalar()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-6] Dispose 후 ExecuteReader() 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();

                bool exceptionThrown = false;

                try
                {
                    context.ExecuteReader(
                        "SELECT 1");
                }
                catch (ObjectDisposedException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Dispose 후 ExecuteReader()에서 " +
                        "ObjectDisposedException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    [15-7] Dispose 두 번 호출 테스트");

            {
                SqliteConnectionFactory connectionFactory =
                    new SqliteConnectionFactory(connectionString);

                SqliteParameterFactory parameterFactory =
                    new SqliteParameterFactory();

                SqliteDialect dialect =
                    new SqliteDialect();

                DatabaseContext context =
                    new DatabaseContext(
                        connectionFactory,
                        parameterFactory,
                        dialect);

                context.Dispose();
                context.Dispose();

                Console.WriteLine(
                    "        Dispose() 두 번째 호출 : 정상");

                Console.WriteLine("        OK");
            }

            Console.WriteLine();
            Console.WriteLine(
                "    DatabaseContext Dispose 테스트 PASS");
        }


       
        private static void TestDatabaseContextSqlExceptions(
            string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine(
                "[16] DatabaseContext SQL 예외 테스트");

            SqliteConnectionFactory connectionFactory =
                new SqliteConnectionFactory(connectionString);

            SqliteParameterFactory parameterFactory =
                new SqliteParameterFactory();

            SqliteDialect dialect =
                new SqliteDialect();

            DatabaseContext context =
                new DatabaseContext(
                    connectionFactory,
                    parameterFactory,
                    dialect);

            try
            {
                Console.WriteLine();
                Console.WriteLine(
                    "    [16-1] 빈 SQL 예외 테스트");

                bool exceptionThrown = false;

                try
                {
                    context.ExecuteNonQuery("");
                }
                catch (ArgumentException ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().Name);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "빈 SQL에 대한 " +
                        "ArgumentException이 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    [16-2] 잘못된 SQL 예외 테스트");

                exceptionThrown = false;

                try
                {
                    context.ExecuteNonQuery(
                        "INVALID SQL");
                }
                catch (Exception ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().FullName);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "잘못된 SQL에 대한 " +
                        "예외가 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    [16-3] 존재하지 않는 테이블 예외 테스트");

                exceptionThrown = false;

                try
                {
                    context.ExecuteScalar(
                        "SELECT COUNT(*) " +
                        "FROM TableThatDoesNotExist");
                }
                catch (Exception ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().FullName);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "존재하지 않는 테이블에 대한 " +
                        "예외가 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    [16-4] Parameter 불일치 예외 테스트");

                exceptionThrown = false;

                try
                {
                    IDbDataParameter parameter =
                        context.CreateParameter(
                            "@id",
                            1);

                    context.ExecuteScalar(
                        "SELECT @missing",
                        new IDbDataParameter[]
                        {
                    parameter
                        });
                }
                catch (Exception ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().FullName);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "Parameter 불일치에 대한 " +
                        "예외가 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    [16-5] 중복 Primary Key 예외 테스트");

                context.ExecuteNonQuery(
                    "DROP TABLE IF EXISTS ExceptionTest");

                context.ExecuteNonQuery(
                    "CREATE TABLE ExceptionTest (" +
                    "id INTEGER PRIMARY KEY, " +
                    "name TEXT)");

                context.ExecuteNonQuery(
                    "INSERT INTO ExceptionTest " +
                    "(id, name) " +
                    "VALUES (1, '첫 번째')");

                exceptionThrown = false;

                try
                {
                    IDbDataParameter idParameter =
                        context.CreateParameter(
                            "@id",
                            1);

                    IDbDataParameter nameParameter =
                        context.CreateParameter(
                            "@name",
                            "두 번째");

                    context.ExecuteNonQuery(
                        "INSERT INTO ExceptionTest " +
                        "(id, name) " +
                        "VALUES (@id, @name)",
                        new IDbDataParameter[]
                        {
                    idParameter,
                    nameParameter
                        });
                }
                catch (Exception ex)
                {
                    exceptionThrown = true;

                    Console.WriteLine(
                        "        예상된 예외 : " +
                        ex.GetType().FullName);

                    Console.WriteLine(
                        "        메시지 : " +
                        ex.Message);
                }

                if (!exceptionThrown)
                {
                    throw new Exception(
                        "중복 Primary Key에 대한 " +
                        "예외가 발생하지 않았습니다.");
                }

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    ExceptionTest 테이블 삭제");

                context.ExecuteNonQuery(
                    "DROP TABLE ExceptionTest");

                Console.WriteLine("        OK");

                Console.WriteLine();
                Console.WriteLine(
                    "    DatabaseContext SQL 예외 테스트 PASS");
            }
            finally
            {
                context.Dispose();
            }
        }





        //private static IDbDataParameter CreateParameter(
        //    GenericRepository<TestUser> repository,
        //    string name,
        //    object value)
        //{
        //    /*
        //     * 현재 GenericRepository API는
        //     * IDbDataParameter를 직접 받도록 되어 있습니다.
        //     *
        //     * 따라서 테스트에서는 SQLiteParameter를 사용합니다.
        //     */
        //    return new SQLiteParameter(
        //        name,
        //        value);
        //}

        #endregion


        private enum TestUserStatus

        {
            Normal = 1,
            Suspended = 2,
            Deleted = 3
        }

        [Table("OrmMappingTest")]
        private class OrmMappingTestEntity
        {
            [Column(
                "id",
                IsPrimaryKey = true,
                IsInsertable = true,
                IsUpdatable = false)]
            public int Id { get; set; }

            [Column("display_name")]
            public string Name { get; set; }

            [Column("age")]
            public int Age { get; set; }

            [Column("nullable_age")]
            public int? NullableAge { get; set; }

            [Column("active")]
            public bool Active { get; set; }

            [Column("status")]
            public TestUserStatus Status { get; set; }

            [Column("created_date")]
            public DateTime CreatedDate { get; set; }

            [Column(
                "date_only",
                StoreDateOnly = true)]
            public DateTime DateOnly { get; set; }

            [Column("nullable_date")]
            public DateTime? NullableDate { get; set; }

            [Column("guid_value")]
            public Guid GuidValue { get; set; }

            [Column("initial")]
            public char Initial { get; set; }

            /*
             * INSERT / UPDATE에서 제외되는 Property입니다.
             *
             * 테이블에는 존재하지만 Entity 값은
             * INSERT / UPDATE SQL에 포함되지 않는지 확인합니다.
             */
            [Column(
                "server_value",
                IsInsertable = false,
                IsUpdatable = false)]
            public string ServerValue { get; set; }
        }


        [Table("TestUser")]
        [Index("IX_TestUser_Name", "Name")]
        private sealed class TestUser
        {
            [Column(
                "Id",
                IsPrimaryKey = true,
                IsInsertable = false,
                IsUpdatable = false)]
            public int Id { get; set; }

            [Column("Name")]
            public string Name { get; set; }

            [Column("Age")]
            public int Age { get; set; }
        }


        [Table("SqlBuilderTest")]
        private class SqlBuilderTestEntity
        {
            [Column(
                "id",
                IsPrimaryKey = true,
                IsInsertable = true,
                IsUpdatable = false)]
            public int Id { get; set; }

            [Column(
                "name",
                IsInsertable = true,
                IsUpdatable = true)]
            public string Name { get; set; }

            [Column(
                "age",
                IsInsertable = true,
                IsUpdatable = true)]
            public int Age { get; set; }

            [Column(
                "server_value",
                IsInsertable = false,
                IsUpdatable = false)]
            public string ServerValue { get; set; }
        }

        
        private static void AddParameter(
            IDbCommand command,
            string name,
            object value)
        {
            IDbDataParameter parameter =
                command.CreateParameter();

            parameter.ParameterName = name;
            parameter.Value =
                value ?? DBNull.Value;

            command.Parameters.Add(parameter);
        }

        
        [Table("RepositoryConditionTest")]
        private class RepositoryConditionTestEntity
        {
            [Column(
                "id",
                IsPrimaryKey = true,
                IsInsertable = true,
                IsUpdatable = false)]
            public int Id { get; set; }

            [Column("name")]
            public string Name { get; set; }

            [Column("age")]
            public int Age { get; set; }

            [Column("category")]
            public string Category { get; set; }

            [Column("active")]
            public bool Active { get; set; }
        }


        private static List<IDbDataParameter> CreateParameters(
            IDbConnection connection,
            params object[] values)
        {
            List<IDbDataParameter> parameters =
                new List<IDbDataParameter>();

            for (int i = 0; i < values.Length; i++)
            {
                IDbCommand command =
                    connection.CreateCommand();

                IDbDataParameter parameter =
                    command.CreateParameter();

                parameter.ParameterName =
                    "@p" + i;

                parameter.Value =
                    values[i] ?? DBNull.Value;

                parameters.Add(parameter);

                command.Dispose();
            }

            return parameters;
        }


    }
}
