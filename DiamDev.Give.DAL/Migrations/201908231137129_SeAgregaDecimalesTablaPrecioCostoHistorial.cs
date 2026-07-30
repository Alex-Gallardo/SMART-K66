namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaDecimalesTablaPrecioCostoHistorial : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Actual", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Nuevo", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Promedio", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Promedio", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Nuevo", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Producto_Precio_Costo_Historial", "Precio_Costo_Actual", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
