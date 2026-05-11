namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaLlavePrimariaUsuarioEmpresa : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Usuario_Empresa");
            AlterColumn("dbo.Usuario_Empresa", "Codigo", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.Usuario_Empresa", new[] { "Usuario_Id", "Empresa_Id", "Codigo" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.Usuario_Empresa");
            AlterColumn("dbo.Usuario_Empresa", "Codigo", c => c.String());
            AddPrimaryKey("dbo.Usuario_Empresa", new[] { "Usuario_Id", "Empresa_Id" });
        }
    }
}
