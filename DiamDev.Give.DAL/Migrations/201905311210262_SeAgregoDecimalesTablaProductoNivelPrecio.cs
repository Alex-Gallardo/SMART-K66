namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoDecimalesTablaProductoNivelPrecio : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Producto_Nivel_Precio", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Producto_Nivel_Precio", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
