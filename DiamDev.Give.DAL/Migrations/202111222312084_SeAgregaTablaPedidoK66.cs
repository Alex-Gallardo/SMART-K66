namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Pedido_Detalle_K66",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Pedido_Id = c.Long(nullable: false),
                        Producto_Id = c.String(),
                        Nombre = c.String(),
                        Unidad = c.String(),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Pedido_Id })
                .ForeignKey("dbo.Pedido_K66", t => t.Pedido_Id, cascadeDelete: true)
                .Index(t => t.Pedido_Id);
            
            CreateTable(
                "dbo.Pedido_K66",
                c => new
                    {
                        Pedido_Id = c.Long(nullable: false),
                        Empresa_Id = c.Long(nullable: false),
                        ID_K66 = c.String(),
                        Nit = c.String(),
                        Nombre = c.String(),
                        Direccion = c.String(),
                        Responsable_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Pedido_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_K66", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Pedido_K66", "Empresa_Id", "dbo.Empresa");
            DropForeignKey("dbo.Pedido_Detalle_K66", "Pedido_Id", "dbo.Pedido_K66");
            DropIndex("dbo.Pedido_K66", new[] { "Responsable_Id" });
            DropIndex("dbo.Pedido_K66", new[] { "Empresa_Id" });
            DropIndex("dbo.Pedido_Detalle_K66", new[] { "Pedido_Id" });
            DropTable("dbo.Pedido_K66");
            DropTable("dbo.Pedido_Detalle_K66");
        }
    }
}
