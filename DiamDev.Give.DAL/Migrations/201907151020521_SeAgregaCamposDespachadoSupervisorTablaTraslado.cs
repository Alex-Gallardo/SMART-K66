namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposDespachadoSupervisorTablaTraslado : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Traslado", "Despachado", c => c.Boolean(nullable: false));
            AddColumn("dbo.Traslado", "Supervisor", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Traslado", "Supervisor");
            DropColumn("dbo.Traslado", "Despachado");
        }
    }
}
