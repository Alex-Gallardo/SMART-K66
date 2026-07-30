namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoDecimalesTablaReciboDetalle : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Recibo_Detalle", "Descuento", c => c.Decimal(precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Recibo_Detalle", "Descuento", c => c.Decimal(precision: 18, scale: 2));
        }
    }
}
