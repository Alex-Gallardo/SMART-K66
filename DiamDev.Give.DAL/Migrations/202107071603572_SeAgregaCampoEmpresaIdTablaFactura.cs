namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaFactura : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Factura", "Empresa_Id");
            AddForeignKey("dbo.Factura", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Factura", new[] { "Empresa_Id" });
            DropColumn("dbo.Factura", "Empresa_Id");
        }
    }
}
