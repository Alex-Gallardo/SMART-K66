namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPedidoDocumentoImportante : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Pedido_Documento_Importante_K66",
                c => new
                    {
                        Documento_Id = c.Int(nullable: false),
                        Pedido_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        FotografiaApp = c.String(),
                    })
                .PrimaryKey(t => new { t.Documento_Id, t.Pedido_Id })
                .ForeignKey("dbo.Pedido_K66", t => t.Pedido_Id, cascadeDelete: true)
                .Index(t => t.Pedido_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_Documento_Importante_K66", "Pedido_Id", "dbo.Pedido_K66");
            DropIndex("dbo.Pedido_Documento_Importante_K66", new[] { "Pedido_Id" });
            DropTable("dbo.Pedido_Documento_Importante_K66");
        }
    }
}
