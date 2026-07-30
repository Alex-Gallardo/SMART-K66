namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaDecimalesTablaCorteCaja : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Corte_Caja", "Monto", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Corte_Caja", "Gasto", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Corte_Caja", "Gasto", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Corte_Caja", "Monto", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
