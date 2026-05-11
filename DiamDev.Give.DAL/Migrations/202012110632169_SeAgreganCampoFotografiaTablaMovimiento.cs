namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCampoFotografiaTablaMovimiento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento", "Fotografia_Movimiento", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento", "Fotografia_Movimiento");
        }
    }
}
