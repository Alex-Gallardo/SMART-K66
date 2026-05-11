namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Politicas : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Politica_Categoria_Politica",
                c => new
                    {
                        Politica_Categoria_Id = c.Long(nullable: false),
                        Politica_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Politica_Categoria_Id, t.Politica_Id })
                .ForeignKey("dbo.Politica", t => t.Politica_Id, cascadeDelete: true)
                .ForeignKey("dbo.Politica_Categoria", t => t.Politica_Categoria_Id, cascadeDelete: true)
                .Index(t => t.Politica_Categoria_Id)
                .Index(t => t.Politica_Id);
            
            CreateTable(
                "dbo.Politica",
                c => new
                    {
                        Politica_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Nombre = c.String(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Politica_Id)
                .ForeignKey("dbo.Politica_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .Index(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Politica_Tipo",
                c => new
                    {
                        Politica_Tipo_Id = c.Int(nullable: false),
                        Nombre = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Politica_Tipo_Id);
            
            CreateTable(
                "dbo.Politica_Categoria",
                c => new
                    {
                        Politica_Categoria_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Nombre = c.String(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Politica_Categoria_Id)
                .ForeignKey("dbo.Politica_Tipo", t => t.Tipo_Id)
                .Index(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Reparacion_Politica_Categoria",
                c => new
                    {
                        Reparacion_Id = c.Long(nullable: false),
                        Politica_Categoria_Id = c.Long(nullable: false),
                        Orden_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Reparacion_Id, t.Politica_Categoria_Id })
                .ForeignKey("dbo.Politica_Categoria", t => t.Politica_Categoria_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .Index(t => t.Reparacion_Id)
                .Index(t => t.Politica_Categoria_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reparacion_Politica_Categoria", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion_Politica_Categoria", "Politica_Categoria_Id", "dbo.Politica_Categoria");
            DropForeignKey("dbo.Politica_Categoria", "Tipo_Id", "dbo.Politica_Tipo");
            DropForeignKey("dbo.Politica_Categoria_Politica", "Politica_Categoria_Id", "dbo.Politica_Categoria");
            DropForeignKey("dbo.Politica_Categoria_Politica", "Politica_Id", "dbo.Politica");
            DropForeignKey("dbo.Politica", "Tipo_Id", "dbo.Politica_Tipo");
            DropIndex("dbo.Reparacion_Politica_Categoria", new[] { "Politica_Categoria_Id" });
            DropIndex("dbo.Reparacion_Politica_Categoria", new[] { "Reparacion_Id" });
            DropIndex("dbo.Politica_Categoria", new[] { "Tipo_Id" });
            DropIndex("dbo.Politica", new[] { "Tipo_Id" });
            DropIndex("dbo.Politica_Categoria_Politica", new[] { "Politica_Id" });
            DropIndex("dbo.Politica_Categoria_Politica", new[] { "Politica_Categoria_Id" });
            DropTable("dbo.Reparacion_Politica_Categoria");
            DropTable("dbo.Politica_Categoria");
            DropTable("dbo.Politica_Tipo");
            DropTable("dbo.Politica");
            DropTable("dbo.Politica_Categoria_Politica");
        }
    }
}
