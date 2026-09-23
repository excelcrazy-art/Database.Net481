using Database.Net481.Attributes;

namespace Database.Net481.ConsoleTest.Models
{
    [Table("TestUser")]
    [Index("IX_TestUser_Name", "name")]
    [Index("IX_TestUser_Email", "email", IsUnique = true)]
    public class TestUser
    {
        [Column("id", IsPrimaryKey = true, IsInsertable = false, IsUpdatable = false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("email")]
        public string Email { get; set; }
    }
}
