namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCampoFechaDocumentoTablaMovimiento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento", "Fecha_Documento", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento", "Fecha_Documento");
        }
    }
}
