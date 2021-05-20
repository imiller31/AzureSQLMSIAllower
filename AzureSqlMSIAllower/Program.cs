using System;
using YamlDotNet.Serialization;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace AzureSqlMSIAllower
{
    class Program
    {
        private const string allowCmd = @"
IF NOT EXISTS (SELECT 1 from sys.database_principals where name = '{0}')
    DECLARE @cmd nvarchar(4000);
    SET @cmd = 'CREATE USER [{0}] WITH SID = '+CONVERT(VARCHAR(1000), CAST(CAST('{1}' AS UNIQUEIDENTIFIER) AS varbinary(16)), 1)+', TYPE=E';
    EXEC sp_executesql @cmd;
    ALTER ROLE db_owner ADD MEMBER [{0}];
SELECT COUNT(*) from sys.database_principals where name = '{0}';";

        static int Main(string[] args)
        {
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Error: File does not exist!");
                return 2;
            }
            string yaml = File.ReadAllText(args[0]);
            var deserializer = new DeserializerBuilder()
                .Build();

            var sqlConfig = deserializer.Deserialize<SqlConfig>(yaml);

            foreach (KeyValuePair<string, Dictionary <string, string>> db in sqlConfig.databases)
            {
                foreach (KeyValuePair<string, string> msi in db.Value)
                {
                    using (SqlConnection sqlConn = new SqlConnection(string.Format("Server={0};Authentication=Active Directory MSI;Database={1};UID={2};", sqlConfig.server, db.Key, sqlConfig.msiClientId)))
                    {
                        string commandTxt = string.Format(allowCmd, msi.Key, msi.Value);
                        using (SqlCommand command = new SqlCommand(commandTxt, sqlConn))
                        {
                            sqlConn.Open();
                            int returnCode;
                            try
                            {
                                returnCode = (int)command.ExecuteScalar();
                                if (returnCode != 0)
                                {
                                    Console.WriteLine(string.Format("MSI {0} is added to the DB", msi.Key));
                                }

                            } catch (SqlException e)
                            {
                                if (e.Number == 15063)
                                {
                                    Console.WriteLine(string.Format("MSI {0} is already added to the DB under a different username", msi.Key));
                                } else
                                {
                                    Console.WriteLine("Encountered an error on SQL execution.");
                                    Console.WriteLine(e.ToString());
                                }
                            } finally
                            {
                                sqlConn.Close();
                            }
                        }
                    }
                }
            }

            return 0;
        }
    }
}
