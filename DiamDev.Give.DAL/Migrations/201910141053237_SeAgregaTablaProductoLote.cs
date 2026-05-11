namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaProductoLote : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Producto_Lote",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Agencia_Id = c.Long(nullable: false),
                        Lote = c.String(nullable: false, maxLength: 100),
                        Fecha_Vencimiento = c.DateTime(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Producto_Id, t.Agencia_Id, t.Lote })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id)
                .Index(t => t.Agencia_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto_Lote", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto_Lote", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Producto_Lote", new[] { "Agencia_Id" });
            DropIndex("dbo.Producto_Lote", new[] { "Producto_Id" });
            DropTable("dbo.Producto_Lote");
        }
    }
}
