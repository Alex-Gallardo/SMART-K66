namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SePoneNullCampoFechaVencimientoLoteTablaMovimientoDetalle : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Movimiento_Detalle", "Fecha_Vencimiento_Lote", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Movimiento_Detalle", "Fecha_Vencimiento_Lote", c => c.DateTime(nullable: false));
        }
    }
}
