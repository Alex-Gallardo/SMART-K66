namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposTablaVisita : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Visita", "Bolik", c => c.Boolean(nullable: false));
            AddColumn("dbo.Visita", "Empaques", c => c.Boolean(nullable: false));
            AddColumn("dbo.Visita", "Faes", c => c.Boolean(nullable: false));
            AddColumn("dbo.Visita", "Graco", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Visita", "Graco");
            DropColumn("dbo.Visita", "Faes");
            DropColumn("dbo.Visita", "Empaques");
            DropColumn("dbo.Visita", "Bolik");
        }
    }
}
