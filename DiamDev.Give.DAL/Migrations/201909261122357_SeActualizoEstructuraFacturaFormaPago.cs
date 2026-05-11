namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeActualizoEstructuraFacturaFormaPago : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Factura_Forma_Pago");
            AddColumn("dbo.Factura_Forma_Pago", "Detalle_Id", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.Factura_Forma_Pago", new[] { "Detalle_Id", "Factura_Id" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.Factura_Forma_Pago");
            DropColumn("dbo.Factura_Forma_Pago", "Detalle_Id");
            AddPrimaryKey("dbo.Factura_Forma_Pago", new[] { "Factura_Id", "Forma_Pago_Id" });
        }
    }
}
