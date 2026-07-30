namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaAgenciaConsulta : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Usuario_Agencia_Consulta",
                c => new
                    {
                        Usuario_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Usuario_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usuario_Id, cascadeDelete: true)
                .Index(t => t.Usuario_Id)
                .Index(t => t.Agencia_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Usuario_Agencia_Consulta", "Usuario_Id", "dbo.Usuario");
            DropForeignKey("dbo.Usuario_Agencia_Consulta", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Usuario_Agencia_Consulta", new[] { "Agencia_Id" });
            DropIndex("dbo.Usuario_Agencia_Consulta", new[] { "Usuario_Id" });
            DropTable("dbo.Usuario_Agencia_Consulta");
        }
    }
}
