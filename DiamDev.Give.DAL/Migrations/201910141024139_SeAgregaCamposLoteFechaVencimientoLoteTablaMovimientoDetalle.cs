namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposLoteFechaVencimientoLoteTablaMovimientoDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento_Detalle", "Lote", c => c.String(maxLength: 100));
            AddColumn("dbo.Movimiento_Detalle", "Fecha_Vencimiento_Lote", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento_Detalle", "Fecha_Vencimiento_Lote");
            DropColumn("dbo.Movimiento_Detalle", "Lote");
        }
    }
}
