namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoFechaPagoEstimadaTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Fecha_Pago_Estimada", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Fecha_Pago_Estimada");
        }
    }
}
