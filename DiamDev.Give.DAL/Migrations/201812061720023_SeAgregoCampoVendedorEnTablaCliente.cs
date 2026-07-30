namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoVendedorEnTablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Vendedor_Id", c => c.Long());
            CreateIndex("dbo.Cliente", "Vendedor_Id");
            AddForeignKey("dbo.Cliente", "Vendedor_Id", "dbo.Vendedor", "Vendedor_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Cliente", new[] { "Vendedor_Id" });
            DropColumn("dbo.Cliente", "Vendedor_Id");
        }
    }
}
