namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablasDeKardexMovimiento : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Kardex_Movimiento",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Agencia_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Fecha_Hora = c.DateTime(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Documento_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Existencia_Actual = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Existencia_Final = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Responsable_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .ForeignKey("dbo.Kardex_Movimiento_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Tipo_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id)
                .Index(t => t.Responsable_Id);
            
            CreateTable(
                "dbo.Kardex_Movimiento_Tipo",
                c => new
                    {
                        Tipo_Id = c.Int(nullable: false),
                        Nombre = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Kardex_Movimiento", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Kardex_Movimiento", "Tipo_Id", "dbo.Kardex_Movimiento_Tipo");
            DropForeignKey("dbo.Kardex_Movimiento", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Kardex_Movimiento", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Kardex_Movimiento", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Kardex_Movimiento", new[] { "Responsable_Id" });
            DropIndex("dbo.Kardex_Movimiento", new[] { "Unidad_Id" });
            DropIndex("dbo.Kardex_Movimiento", new[] { "Producto_Id" });
            DropIndex("dbo.Kardex_Movimiento", new[] { "Tipo_Id" });
            DropIndex("dbo.Kardex_Movimiento", new[] { "Agencia_Id" });
            DropTable("dbo.Kardex_Movimiento_Tipo");
            DropTable("dbo.Kardex_Movimiento");
        }
    }
}
