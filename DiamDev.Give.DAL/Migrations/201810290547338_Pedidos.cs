namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Pedidos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Pedido_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Pedido_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Descuento = c.Decimal(precision: 18, scale: 2),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Pedido_Id })
                .ForeignKey("dbo.Pedido", t => t.Pedido_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Pedido_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Pedido",
                c => new
                    {
                        Pedido_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Descripcion = c.String(nullable: false),
                        Operada = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Opero = c.Long(),
                        Fecha_Opero = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Pedido_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Usuario", t => t.Usr_Opero)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Opero);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Pedido_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Pedido", "Usr_Opero", "dbo.Usuario");
            DropForeignKey("dbo.Pedido", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Pedido_Detalle", "Pedido_Id", "dbo.Pedido");
            DropForeignKey("dbo.Pedido", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Pedido", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Pedido", new[] { "Usr_Opero" });
            DropIndex("dbo.Pedido", new[] { "Usr_Creo" });
            DropIndex("dbo.Pedido", new[] { "Cliente_Id" });
            DropIndex("dbo.Pedido", new[] { "Agencia_Id" });
            DropIndex("dbo.Pedido_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Pedido_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Pedido_Detalle", new[] { "Pedido_Id" });
            DropTable("dbo.Pedido");
            DropTable("dbo.Pedido_Detalle");
        }
    }
}
