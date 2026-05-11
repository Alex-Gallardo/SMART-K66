namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PrecisionDecimalesCampoPorcentajeTablaVendedorEscala : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Vendedor_Escala", "Porcentaje", c => c.Decimal(nullable: false, precision: 18, scale: 5));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Vendedor_Escala", "Porcentaje", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
