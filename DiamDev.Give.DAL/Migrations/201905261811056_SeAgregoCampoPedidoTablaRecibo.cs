namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoPedidoTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Pedido_Id", c => c.Long());
            CreateIndex("dbo.Recibo", "Pedido_Id");
            AddForeignKey("dbo.Recibo", "Pedido_Id", "dbo.Pedido", "Pedido_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo", "Pedido_Id", "dbo.Pedido");
            DropIndex("dbo.Recibo", new[] { "Pedido_Id" });
            DropColumn("dbo.Recibo", "Pedido_Id");
        }
    }
}
