namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaProductosEspeciales : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Empresa_Producto_Especial",
                c => new
                    {
                        Especial_Id = c.Guid(nullable: false, identity: true),
                        Empresa_Id = c.Long(nullable: false),
                        Codigo = c.String(),
                        Nombre = c.String(),
                        Unidad = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Responsable_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Especial_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Empresa_Producto_Especial", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Empresa_Producto_Especial", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Empresa_Producto_Especial", new[] { "Responsable_Id" });
            DropIndex("dbo.Empresa_Producto_Especial", new[] { "Empresa_Id" });
            DropTable("dbo.Empresa_Producto_Especial");
        }
    }
}
