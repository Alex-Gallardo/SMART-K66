namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaNumeroIngresoTablaHistorialPrecioCosto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto_Precio_Costo_Historial", "Ingreso_Id", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Producto_Precio_Costo_Historial", "Ingreso_Id");
        }
    }
}
