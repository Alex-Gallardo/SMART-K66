namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposPedidoK66Control : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Termino_Entrega", c => c.String());
            AddColumn("dbo.Pedido_K66", "Vendedor", c => c.String());
            AddColumn("dbo.Pedido_K66", "Impuesto_TAX", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_K66", "Impuesto_TAX");
            DropColumn("dbo.Pedido_K66", "Vendedor");
            DropColumn("dbo.Pedido_K66", "Termino_Entrega");
        }
    }
}
