using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Database.Net481.Attributes;
using Database.Net481.Database;
using Database.Net481.Schema;
using Database.Net481.Schema.Dialects;
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

                using (System.Data.IDataReader reader =
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
                Console.WriteLine("    Parameter 조회");

                IDbDataParameter parameter =
                    new SQLiteParameter("@Age",40);

                using (System.Data.IDataReader reader =
                    context.ExecuteReader(
                        @"SELECT Id, Name, Age
                  FROM TestUser
                  WHERE Age >= @Age
                  ORDER BY Id",
                        new System.Data.IDbDataParameter[]
                        {
                    parameter
                        }))
                {
                    count = 0;

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

                if (count != 2)
                {
                    throw new Exception(
                        "Parameter 조회 결과가 예상과 다릅니다. " +
                        "예상: 2, 실제: " + count);
                }

                Console.WriteLine(
                    "    OK - Parameter 조회 결과 : " + count);

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


    }
}