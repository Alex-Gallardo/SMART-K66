namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaDiasYFechaVencimientoTablaProveedorMovimienot : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proveedor_Movimiento", "Dias_Credito", c => c.Int());
            AddColumn("dbo.Proveedor_Movimiento", "Fecha_Vencimiento", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proveedor_Movimiento", "Fecha_Vencimiento");
            DropColumn("dbo.Proveedor_Movimiento", "Dias_Credito");
        }
    }
}
