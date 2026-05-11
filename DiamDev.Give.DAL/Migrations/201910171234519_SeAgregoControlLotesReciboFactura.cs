namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoControlLotesReciboFactura : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Factura_Lote",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Factura_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Lote = c.String(maxLength: 100),
                        Fecha_Vencimiento = c.DateTime(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Factura_Id })
                .ForeignKey("dbo.Factura", t => t.Factura_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .Index(t => t.Factura_Id)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Recibo_Lote",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Recibo_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Lote = c.String(maxLength: 100),
                        Fecha_Vencimiento = c.DateTime(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Recibo_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id, cascadeDelete: true)
                .Index(t => t.Recibo_Id)
                .Index(t => t.Producto_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo_Lote", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Recibo_Lote", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Factura_Lote", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Factura_Lote", "Factura_Id", "dbo.Factura");
            DropIndex("dbo.Recibo_Lote", new[] { "Producto_Id" });
            DropIndex("dbo.Recibo_Lote", new[] { "Recibo_Id" });
            DropIndex("dbo.Factura_Lote", new[] { "Producto_Id" });
            DropIndex("dbo.Factura_Lote", new[] { "Factura_Id" });
            DropTable("dbo.Recibo_Lote");
            DropTable("dbo.Factura_Lote");
        }
    }
}
