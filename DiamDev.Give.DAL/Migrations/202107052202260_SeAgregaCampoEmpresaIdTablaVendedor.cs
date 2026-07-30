namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaVendedor : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Vendedor", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Vendedor", "Empresa_Id");
            AddForeignKey("dbo.Vendedor", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Vendedor", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Vendedor", new[] { "Empresa_Id" });
            DropColumn("dbo.Vendedor", "Empresa_Id");
        }
    }
}
