namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregrCampoPaswordClient : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Pass", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cliente", "Pass");
        }
    }
}
