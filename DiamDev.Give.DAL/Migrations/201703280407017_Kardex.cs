namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Kardex : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Kardex",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FechaHora = c.DateTime(nullable: false),
                        ProductoId = c.String(nullable: false, maxLength: 50),
                        ProductoCodigo = c.String(maxLength: 250),
                        MarcaId = c.Long(nullable: false),
                        MarcaNombre = c.String(maxLength: 300),
                        Descripcion = c.String(maxLength: 500),
                        Fecha = c.DateTime(nullable: false),
                        DocumentoNumero = c.String(maxLength: 200),
                        Concepto = c.String(maxLength: 500),
                        AgenciaId = c.Long(nullable: false),
                        AgenciaNombre = c.String(maxLength: 300),
                        TipoRegistro = c.String(maxLength: 50),
                        IngresoBodega = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IngresoTienda = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IngresoTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IngresoCostoBodega = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IngresoCostoTienda = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IngresoCostoTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalidaCostoBodega = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalidaCostoTienda = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalidaCostoTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalodaCantidadBodega = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalidaCantidadTienda = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SalidaCantidadTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ExistenciaFinalBodega = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ExistenciaFinalTienda = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ExistenciaFinalTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Kardex");
        }
    }
}
