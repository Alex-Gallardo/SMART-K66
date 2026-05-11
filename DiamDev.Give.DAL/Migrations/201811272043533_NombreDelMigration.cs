namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NombreDelMigration : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Reparto", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "Reparto");
        }
    }
}
