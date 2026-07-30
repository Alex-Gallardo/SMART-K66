namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnasDocumentoDiasCreditoyFechaVencimientoTablaMovimiento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento", "Documento", c => c.String(maxLength: 150));
            AddColumn("dbo.Movimiento", "Dias_Credito", c => c.Int());
            AddColumn("dbo.Movimiento", "Fecha_Vencimiento", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento", "Fecha_Vencimiento");
            DropColumn("dbo.Movimiento", "Dias_Credito");
            DropColumn("dbo.Movimiento", "Documento");
        }
    }
}
