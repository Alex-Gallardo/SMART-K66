namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoEmpresaIdTablaFormaPago : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Forma_Pago", "Empresa_Id", c => c.Long());
            CreateIndex("dbo.Forma_Pago", "Empresa_Id");
            AddForeignKey("dbo.Forma_Pago", "Empresa_Id", "dbo.Empresa", "Empresa_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Forma_Pago", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Forma_Pago", new[] { "Empresa_Id" });
            DropColumn("dbo.Forma_Pago", "Empresa_Id");
        }
    }
}
