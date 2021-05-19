using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AzureSqlMSIAllower
{
    public class SqlConfig
    {
        public string server { get; set; }
        public string msiClientId { get; set; }
        public Dictionary<string, Dictionary<string, string>> databases { get; set; }
    }
}
