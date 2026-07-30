namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaProductoNiveles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Producto_Nivel_Precio",
                c => new
                    {
                        Nivel_Id = c.Int(nullable: false),
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Inicial = c.Int(nullable: false),
                        Final = c.Int(nullable: false),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Nivel_Id, t.Producto_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto_Nivel_Precio", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Producto_Nivel_Precio", new[] { "Producto_Id" });
            DropTable("dbo.Producto_Nivel_Precio");
        }
    }
}
