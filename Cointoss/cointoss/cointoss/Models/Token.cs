using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace cointoss.Models
{
    public class Token
    {
        public int ID { get; set; }
        public string OAuthToken { get; set; }
        public string Cost { get; set; }
        public string InitialGuess { get; set; }
        public string EffectiveResult { get; set; }
        public string BetID { get; set; }
    }

    public class TokenDBContext : DbContext
    {
        public DbSet<Token> Tokens { get; set; }
    }

}