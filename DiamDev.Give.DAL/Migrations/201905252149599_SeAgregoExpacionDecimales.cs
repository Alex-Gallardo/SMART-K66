namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoExpacionDecimales : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Producto_Precio", "Valor", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Recibo_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Recibo_Detalle", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Recibo_Forma_Pago", "Valor", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Recibo_Forma_Pago", "Valor", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Recibo_Detalle", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Recibo_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Producto_Precio", "Valor", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
