namespace cointoss.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddInitialGuess : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tokens", "InitialGuess", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tokens", "InitialGuess");
        }
    }
}
