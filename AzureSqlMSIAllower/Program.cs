﻿using System;
using YamlDotNet.Serialization;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace AzureSqlMSIAllower
{
    class Program
    {
        private const string allowCmd = @"
IF (SELECT 1 from sys.database_principals where name = [{0}])
BEGIN
DECLARE @cmd nvarchar(4000)
SET @cmd = 'CREATE USER [{0}] WITH SID = '+CONVERT(VARCHAR(1000), CAST(CAST('{1}' AS UNIQUEIDENTIFIER) AS varbinary(16)), 1)+', TYPE=E'
EXEC sp_executesql @cmd
ALTER ROLE db_owner ADD MEMBER [{0}]
RETURN 0
END
RETURN 1";

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
            Console.WriteLine(sqlConfig.ToString());

            ActiveDirectoryAuthenticationProvider provider = new ActiveDirectoryAuthenticationProvider(sqlConfig.msiClientId);
            if (provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryMSI))
            {
                SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI, provider);
            }

            foreach (KeyValuePair<string, Dictionary <string, string>> db in sqlConfig.databases)
            {

                using (SqlConnection sqlConn = new SqlConnection(string.Format("Server={0};Authentication=Active Directory MSI;Database={1};", sqlConfig.server, db.Key)))
                {
                    foreach (KeyValuePair<string, string> msi in db.Value)
                    {
                        string commandTxt = string.Format(allowCmd, msi.Key, msi.Value);
                        using (SqlCommand command = new SqlCommand(commandTxt, sqlConn))
                        {
                            sqlConn.Open();
                            var returnCode = (int) command.ExecuteScalar();
                            if (returnCode != 0) {
                                Console.WriteLine(string.Format("MSI {0} is already added to the DB", msi.Key));
                            }
                        }
                    }
                }
            }

            return 0;
        }
    }
}