namespace cointoss.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBetId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tokens", "BetID", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tokens", "BetID");
        }
    }
}
