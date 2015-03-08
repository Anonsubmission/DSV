namespace cointoss.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPrice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tokens", "Cost", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tokens", "Cost");
        }
    }
}
