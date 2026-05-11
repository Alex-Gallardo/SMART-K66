namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoDecimalesKardexMovimiento : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Kardex_Movimiento", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Kardex_Movimiento", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
