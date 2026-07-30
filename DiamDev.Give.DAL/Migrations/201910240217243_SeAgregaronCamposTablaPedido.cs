namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaronCamposTablaPedido : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido", "Forma_Pago", c => c.String(maxLength: 500));
            AddColumn("dbo.Pedido", "Tiempo_Entrega", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido", "Tiempo_Entrega");
            DropColumn("dbo.Pedido", "Forma_Pago");
        }
    }
}
