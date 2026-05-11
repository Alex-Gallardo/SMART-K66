namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablasEgresoEgresoDetalle : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Egreso_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Egreso_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ID = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Egreso_Id })
                .ForeignKey("dbo.Egreso", t => t.Egreso_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Egreso_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Egreso",
                c => new
                    {
                        Egreso_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Observaciones = c.String(),
                        Usr_Inicial = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Egreso_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Inicial, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Usr_Inicial);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Egreso_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Egreso_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Egreso", "Usr_Inicial", "dbo.Usuario");
            DropForeignKey("dbo.Egreso_Detalle", "Egreso_Id", "dbo.Egreso");
            DropForeignKey("dbo.Egreso", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Egreso", new[] { "Usr_Inicial" });
            DropIndex("dbo.Egreso", new[] { "Agencia_Id" });
            DropIndex("dbo.Egreso_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Egreso_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Egreso_Detalle", new[] { "Egreso_Id" });
            DropTable("dbo.Egreso");
            DropTable("dbo.Egreso_Detalle");
        }
    }
}
