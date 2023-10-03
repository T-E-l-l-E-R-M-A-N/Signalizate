using System;

namespace Backend.Server
{
    public class TokenParameters
    {
        public string Issuer => "issuer";
        public string Audience => "audience";
        public string SecretKey => "yr72387387y85y238uh5u3uh87u8fhjuhwsuhf*8hriuh";

        public DateTime Expiry => DateTime.Now.AddDays(1);
    }
}