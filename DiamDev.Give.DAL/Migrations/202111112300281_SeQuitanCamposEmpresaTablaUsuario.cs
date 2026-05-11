namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeQuitanCamposEmpresaTablaUsuario : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Usuario", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Usuario", new[] { "Empresa_Id" });
            DropColumn("dbo.Usuario", "Empresa_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Usuario", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Usuario", "Empresa_Id");
            AddForeignKey("dbo.Usuario", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
    }
}
