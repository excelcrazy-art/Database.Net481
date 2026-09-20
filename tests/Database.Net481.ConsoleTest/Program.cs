using System;
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