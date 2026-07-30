namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaFechaHoraFacturaATablaFactura : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Fecha_Hora_Factura", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "Fecha_Hora_Factura");
        }
    }
}
