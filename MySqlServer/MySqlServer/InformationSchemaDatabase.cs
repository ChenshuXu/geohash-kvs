using System;
namespace MySqlServer
{
    public class InformationSchemaDatabase : Database
    {
        public InformationSchemaDatabase(string databaseName = "information_schema") : base (databaseName)
        {
            // information schema variable table
            // build information schema
            Table informationSchema = new Table("information schema");
            AddTable(informationSchema);
            informationSchema.AddColumns(new Column[]
            {
                new Column("@@max_allowed_packet", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("@@character_set_client"),
                new Column("@@character_set_connection"),
                new Column("@@license"),
                new Column("@@sql_mode"),
                new Column("@@lower_case_table_names"),
                new Column("@@version_comment")
            });

            informationSchema.AddRow(
                new Row( new Object[]
                {
                    4194304,
                    "utf8",
                    "utf8",
                    "GPL",
                    "ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION",
                    "2",
                    "MySQL Community Server (GPL)"
                })
            );



            // build collation table
            Table collation = new Table("COLLATIONS");
            AddTable(collation);
            collation.DatabaseName = "information_schema";
            collation.AddColumns(new Column[]
            {
                new Column("Collation"),
                new Column("Charset"),
                new Column("Id", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("Default"),
                new Column("Compiled"),
                new Column("Sortlen", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG)
            });

            string path = "../../../Resources/Collations.csv";
            string[] lines = System.IO.File.ReadAllLines(path);
            //Console.WriteLine(lines[0]);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] columns = line.Split('|');
                //Console.WriteLine("--{0}--", columns[1].Trim());
                collation.AddRow(new Row(new Object[]
                {
                    columns[1].Trim(),
                    columns[2].Trim(),
                    int.Parse(columns[3].Trim()),
                    columns[4].Trim(),
                    columns[5].Trim(),
                    int.Parse(columns[6].Trim())
                }));
            }

        }
    }
}
