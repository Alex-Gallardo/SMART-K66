namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaVendedorId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido", "Vendedor_Id", c => c.Long());
            CreateIndex("dbo.Pedido", "Vendedor_Id");
            AddForeignKey("dbo.Pedido", "Vendedor_Id", "dbo.Vendedor", "Vendedor_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Pedido", new[] { "Vendedor_Id" });
            DropColumn("dbo.Pedido", "Vendedor_Id");
        }
    }
}
