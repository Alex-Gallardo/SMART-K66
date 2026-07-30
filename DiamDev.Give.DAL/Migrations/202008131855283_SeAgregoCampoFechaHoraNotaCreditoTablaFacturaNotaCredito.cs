namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoFechaHoraNotaCreditoTablaFacturaNotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Nota_Credito", "Fecha_Hora_Nota_Credito", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura_Nota_Credito", "Fecha_Hora_Nota_Credito");
        }
    }
}
