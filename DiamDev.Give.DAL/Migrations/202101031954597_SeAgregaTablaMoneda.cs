namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaMoneda : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Moneda",
                c => new
                    {
                        Moneda_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Descripcion = c.String(),
                        Tipo_De_Cambio_Compra = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Tipo_De_Cambio_Venta = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Moneda_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Moneda");
        }
    }
}
