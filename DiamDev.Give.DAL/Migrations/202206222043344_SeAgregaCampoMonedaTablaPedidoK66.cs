namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoMonedaTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Moneda", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_K66", "Moneda");
        }
    }
}
