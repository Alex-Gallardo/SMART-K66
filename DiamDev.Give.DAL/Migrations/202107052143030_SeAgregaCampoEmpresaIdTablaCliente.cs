namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Cliente", "Empresa_Id");
            AddForeignKey("dbo.Cliente", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Cliente", new[] { "Empresa_Id" });
            DropColumn("dbo.Cliente", "Empresa_Id");
        }
    }
}
