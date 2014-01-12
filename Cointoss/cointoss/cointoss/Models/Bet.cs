using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace cointoss.Models
{
    public class Bet
    {
        public int ID { get; set; }
        public string guess { get; set; }
        public int amount { get; set; } 
    }

    public class BetDbContext : TokenDBContext
    {
        public DbSet<Bet> Bets { get; set; }
    }
}