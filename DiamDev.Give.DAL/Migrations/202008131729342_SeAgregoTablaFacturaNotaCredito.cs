namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaFacturaNotaCredito : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Factura_Nota_Credito_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Factura_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 400),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Factura_Id })
                .ForeignKey("dbo.Factura_Nota_Credito", t => t.Factura_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Factura_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Factura_Nota_Credito",
                c => new
                    {
                        Factura_Id = c.Long(nullable: false),
                        Infile = c.Boolean(nullable: false),
                        Cantidad_Errores_FEL = c.Int(nullable: false),
                        Descripcion_FEL = c.String(),
                        Fecha_Hora_Certificacion_FEL = c.String(),
                        XML_Certificado_FEL = c.String(),
                        Json_FEL = c.String(),
                        Usr_Creo = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Factura_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Usr_Creo);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura_Nota_Credito_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Factura_Nota_Credito_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Factura_Nota_Credito", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Factura_Nota_Credito_Detalle", "Factura_Id", "dbo.Factura_Nota_Credito");
            DropIndex("dbo.Factura_Nota_Credito", new[] { "Usr_Creo" });
            DropIndex("dbo.Factura_Nota_Credito_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Factura_Nota_Credito_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Factura_Nota_Credito_Detalle", new[] { "Factura_Id" });
            DropTable("dbo.Factura_Nota_Credito");
            DropTable("dbo.Factura_Nota_Credito_Detalle");
        }
    }
}
