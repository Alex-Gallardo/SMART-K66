namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoGarantia : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Garantia_Documento",
                c => new
                    {
                        Documento_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 250),
                    })
                .PrimaryKey(t => t.Documento_Id);
            
            CreateTable(
                "dbo.Garantia",
                c => new
                    {
                        Garantia_Id = c.Long(nullable: false),
                        Documento_Id = c.Int(nullable: false),
                        Factura_Id = c.Long(),
                        Recibo_Id = c.Long(),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Observaciones = c.String(),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Entrega = c.Long(),
                        Fecha_Entrega = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Garantia_Id)
                .ForeignKey("dbo.Garantia_Documento", t => t.Documento_Id, cascadeDelete: true)
                .ForeignKey("dbo.Factura", t => t.Factura_Id)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Usuario", t => t.Usr_Entrega)
                .Index(t => t.Documento_Id)
                .Index(t => t.Factura_Id)
                .Index(t => t.Recibo_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Entrega);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Garantia", "Usr_Entrega", "dbo.Usuario");
            DropForeignKey("dbo.Garantia", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Garantia", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Garantia", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Garantia", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Garantia", "Factura_Id", "dbo.Factura");
            DropForeignKey("dbo.Garantia", "Documento_Id", "dbo.Garantia_Documento");
            DropIndex("dbo.Garantia", new[] { "Usr_Entrega" });
            DropIndex("dbo.Garantia", new[] { "Usr_Creo" });
            DropIndex("dbo.Garantia", new[] { "Unidad_Id" });
            DropIndex("dbo.Garantia", new[] { "Producto_Id" });
            DropIndex("dbo.Garantia", new[] { "Recibo_Id" });
            DropIndex("dbo.Garantia", new[] { "Factura_Id" });
            DropIndex("dbo.Garantia", new[] { "Documento_Id" });
            DropTable("dbo.Garantia");
            DropTable("dbo.Garantia_Documento");
        }
    }
}
