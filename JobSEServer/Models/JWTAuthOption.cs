using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public class ApiKey
    {
        public string Audience { get; set; }
        public string Key { get; set; }
        public DateTime ExpireTime { get; set; }
    }

    public class JWTAuthOption
    {
        public string PrivateKey { get; set; }
        public string Issuer { get; set; }
        public List<ApiKey> ApiKeys { get; set; }
    }
}
