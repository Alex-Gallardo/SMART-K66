namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoDecimalesTablaCierreDetalle : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Cierre_Detalle", "Monto_Sistema", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Cierre_Detalle", "Monto_Cajero", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Cierre_Detalle", "Monto_Cajero", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Cierre_Detalle", "Monto_Sistema", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
