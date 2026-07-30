namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaVistaAReparacionAnotacion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reparacion_Anotacion", "Visto", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reparacion_Anotacion", "Visto");
        }
    }
}
