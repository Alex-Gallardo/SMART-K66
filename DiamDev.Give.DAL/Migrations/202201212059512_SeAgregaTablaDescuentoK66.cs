namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaDescuentoK66 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Descuento_K66",
                c => new
                    {
                        Descuento_Id = c.Guid(nullable: false, identity: true),
                        Empresa_Id = c.Long(nullable: false),
                        ID_K66 = c.String(),
                        Nit = c.String(),
                        Nombre = c.String(),
                        Direccion_Id = c.Int(),
                        Direccion = c.String(),
                        Producto_Id = c.String(),
                        Producto = c.String(),
                        Descuento = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Responsable_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Descuento_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Descuento_K66", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Descuento_K66", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Descuento_K66", new[] { "Responsable_Id" });
            DropIndex("dbo.Descuento_K66", new[] { "Empresa_Id" });
            DropTable("dbo.Descuento_K66");
        }
    }
}
