namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaProducto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Producto", "Empresa_Id");
            AddForeignKey("dbo.Producto", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Producto", new[] { "Empresa_Id" });
            DropColumn("dbo.Producto", "Empresa_Id");
        }
    }
}
