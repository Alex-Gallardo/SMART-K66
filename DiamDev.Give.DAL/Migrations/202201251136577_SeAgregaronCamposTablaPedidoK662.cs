namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaronCamposTablaPedidoK662 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Estado_Id", c => c.Int());
            CreateIndex("dbo.Pedido_K66", "Estado_Id");
            AddForeignKey("dbo.Pedido_K66", "Estado_Id", "dbo.Estado_Smart_K66", "Estado_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_K66", "Estado_Id", "dbo.Estado_Smart_K66");
            DropIndex("dbo.Pedido_K66", new[] { "Estado_Id" });
            DropColumn("dbo.Pedido_K66", "Estado_Id");
        }
    }
}
