namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablasDeOrdenCompra : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Orden_Compra",
                c => new
                    {
                        Orden_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Proveedor_Id = c.Long(nullable: false),
                        Moneda_Id = c.Long(nullable: false),
                        Observaciones = c.String(),
                        Comentario = c.String(),
                        Fotografia_Orden = c.String(),
                        Operado = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Orden_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Moneda", t => t.Moneda_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Moneda_Id)
                .Index(t => t.Usr_Creo);
            
            CreateTable(
                "dbo.Orden_Compra_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Orden_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 400),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Orden_Id })
                .ForeignKey("dbo.Orden_Compra", t => t.Orden_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Orden_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orden_Compra", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Orden_Compra", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Orden_Compra", "Moneda_Id", "dbo.Moneda");
            DropForeignKey("dbo.Orden_Compra_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Orden_Compra_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Orden_Compra_Detalle", "Orden_Id", "dbo.Orden_Compra");
            DropForeignKey("dbo.Orden_Compra", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Orden_Compra_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Orden_Compra_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Orden_Compra_Detalle", new[] { "Orden_Id" });
            DropIndex("dbo.Orden_Compra", new[] { "Usr_Creo" });
            DropIndex("dbo.Orden_Compra", new[] { "Moneda_Id" });
            DropIndex("dbo.Orden_Compra", new[] { "Proveedor_Id" });
            DropIndex("dbo.Orden_Compra", new[] { "Agencia_Id" });
            DropTable("dbo.Orden_Compra_Detalle");
            DropTable("dbo.Orden_Compra");
        }
    }
}
