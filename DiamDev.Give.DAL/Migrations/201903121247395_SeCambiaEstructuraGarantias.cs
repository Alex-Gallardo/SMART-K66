namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCambiaEstructuraGarantias : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Garantia", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Garantia", "Unidad_Id", "dbo.Unidad");
            DropIndex("dbo.Garantia", new[] { "Producto_Id" });
            DropIndex("dbo.Garantia", new[] { "Unidad_Id" });
            CreateTable(
                "dbo.Garantia_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Garantia_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        ID = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Garantia_Id })
                .ForeignKey("dbo.Garantia", t => t.Garantia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Garantia_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            DropColumn("dbo.Garantia", "Producto_Id");
            DropColumn("dbo.Garantia", "Unidad_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Garantia", "Unidad_Id", c => c.Long(nullable: false));
            AddColumn("dbo.Garantia", "Producto_Id", c => c.String(maxLength: 50));
            DropForeignKey("dbo.Garantia_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Garantia_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Garantia_Detalle", "Garantia_Id", "dbo.Garantia");
            DropIndex("dbo.Garantia_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Garantia_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Garantia_Detalle", new[] { "Garantia_Id" });
            DropTable("dbo.Garantia_Detalle");
            CreateIndex("dbo.Garantia", "Unidad_Id");
            CreateIndex("dbo.Garantia", "Producto_Id");
            AddForeignKey("dbo.Garantia", "Unidad_Id", "dbo.Unidad", "Unidad_Id", cascadeDelete: true);
            AddForeignKey("dbo.Garantia", "Producto_Id", "dbo.Producto", "Producto_Id");
        }
    }
}
