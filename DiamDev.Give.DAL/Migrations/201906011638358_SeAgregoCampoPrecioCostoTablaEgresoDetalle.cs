namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoPrecioCostoTablaEgresoDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Egreso_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Egreso_Detalle", "Precio_Costo");
        }
    }
}
