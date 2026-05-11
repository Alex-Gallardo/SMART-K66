namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaHistorialPrecioCosto : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Producto_Precio_Costo_Historial",
                c => new
                    {
                        Historial_Id = c.Long(nullable: false, identity: true),
                        Proveedor_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Precio_Costo_Actual = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo_Nuevo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo_Promedio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Historial_Id)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Producto_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto_Precio_Costo_Historial", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Producto_Precio_Costo_Historial", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Producto_Precio_Costo_Historial", new[] { "Producto_Id" });
            DropIndex("dbo.Producto_Precio_Costo_Historial", new[] { "Proveedor_Id" });
            DropTable("dbo.Producto_Precio_Costo_Historial");
        }
    }
}
