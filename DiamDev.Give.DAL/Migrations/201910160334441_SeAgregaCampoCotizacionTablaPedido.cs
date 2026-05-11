namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoCotizacionTablaPedido : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido", "Cotizacion", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido", "Cotizacion");
        }
    }
}
