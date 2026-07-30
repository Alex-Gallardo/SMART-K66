namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaUsuarioEmpresa : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Usuario_Empresa",
                c => new
                    {
                        Usuario_Id = c.Long(nullable: false),
                        Empresa_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Usuario_Id, t.Empresa_Id })
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usuario_Id, cascadeDelete: true)
                .Index(t => t.Usuario_Id)
                .Index(t => t.Empresa_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Usuario_Empresa", "Usuario_Id", "dbo.Usuario");
            DropForeignKey("dbo.Usuario_Empresa", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Usuario_Empresa", new[] { "Empresa_Id" });
            DropIndex("dbo.Usuario_Empresa", new[] { "Usuario_Id" });
            DropTable("dbo.Usuario_Empresa");
        }
    }
}
