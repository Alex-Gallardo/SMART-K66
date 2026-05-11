namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarFacturaCreditoYDiasDeCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Credito", c => c.Boolean(nullable: false));
            AddColumn("dbo.Factura", "Dia_Credito", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "Dia_Credito");
            DropColumn("dbo.Factura", "Credito");
        }
    }
}
