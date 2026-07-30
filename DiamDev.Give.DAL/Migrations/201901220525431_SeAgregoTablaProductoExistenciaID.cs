namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaProductoExistenciaID : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Producto_Inventario_ID",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        ID = c.String(nullable: false, maxLength: 128),
                        Operado = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.Producto_Id, t.ID })
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto_Inventario_ID", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Producto_Inventario_ID", new[] { "Producto_Id" });
            DropTable("dbo.Producto_Inventario_ID");
        }
    }
}
