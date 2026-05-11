namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaOfertasDelvery : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OfertaDelivery",
                c => new
                    {
                        Oferta_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(nullable: false, maxLength: 300),
                        Fecha = c.DateTime(nullable: false),
                        FechaInicioOferta = c.DateTime(nullable: false),
                        FechaFinOferta = c.DateTime(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        ProductoBase = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Oferta_Id)
                .ForeignKey("dbo.Producto", t => t.ProductoBase)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Usr_Creo)
                .Index(t => t.ProductoBase);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OfertaDelivery", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.OfertaDelivery", "ProductoBase", "dbo.Producto");
            DropIndex("dbo.OfertaDelivery", new[] { "ProductoBase" });
            DropIndex("dbo.OfertaDelivery", new[] { "Usr_Creo" });
            DropTable("dbo.OfertaDelivery");
        }
    }
}
