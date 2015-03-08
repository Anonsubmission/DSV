using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace DotNetOpenAuth.OpenId.RelyingParty
{
    public class CCP
    {
        public int ID { get; set; }
        public string session_id { get; set; }
        public string realm { get; set; }
    }

    public class CCPDbContext : DbContext
    {
        public DbSet<CCP> CCPs { get; set; }
    }
}