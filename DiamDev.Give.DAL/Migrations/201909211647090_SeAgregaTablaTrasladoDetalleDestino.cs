namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaTrasladoDetalleDestino : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Traslado_Detalle_Destino",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Traslado_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ID = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Traslado_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Traslado", t => t.Traslado_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Traslado_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Traslado_Detalle_Destino", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Traslado_Detalle_Destino", "Traslado_Id", "dbo.Traslado");
            DropForeignKey("dbo.Traslado_Detalle_Destino", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Traslado_Detalle_Destino", new[] { "Unidad_Id" });
            DropIndex("dbo.Traslado_Detalle_Destino", new[] { "Producto_Id" });
            DropIndex("dbo.Traslado_Detalle_Destino", new[] { "Traslado_Id" });
            DropTable("dbo.Traslado_Detalle_Destino");
        }
    }
}
