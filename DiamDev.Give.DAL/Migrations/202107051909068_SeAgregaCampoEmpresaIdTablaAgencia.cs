namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaAgencia : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Agencia", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Agencia", "Empresa_Id");
            AddForeignKey("dbo.Agencia", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Agencia", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Agencia", new[] { "Empresa_Id" });
            DropColumn("dbo.Agencia", "Empresa_Id");
        }
    }
}
