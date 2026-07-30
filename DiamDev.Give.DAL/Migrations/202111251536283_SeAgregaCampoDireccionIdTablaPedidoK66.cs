namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoDireccionIdTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Direccion_Id", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_K66", "Direccion_Id");
        }
    }
}
