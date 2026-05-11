namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablasCierreYCierreDetalle : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cierre_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Cierre_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Monto_Sistema = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Monto_Cajero = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Cierre_Id })
                .ForeignKey("dbo.Cierre", t => t.Cierre_Id, cascadeDelete: true)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .Index(t => t.Cierre_Id)
                .Index(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Cierre",
                c => new
                    {
                        Cierre_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cajero_Id = c.Long(nullable: false),
                        Fecha_Hora = c.DateTime(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Cierre_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Cajero_Id, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Cajero_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cierre_Detalle", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Cierre_Detalle", "Cierre_Id", "dbo.Cierre");
            DropForeignKey("dbo.Cierre", "Cajero_Id", "dbo.Usuario");
            DropForeignKey("dbo.Cierre", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Cierre", new[] { "Cajero_Id" });
            DropIndex("dbo.Cierre", new[] { "Agencia_Id" });
            DropIndex("dbo.Cierre_Detalle", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Cierre_Detalle", new[] { "Cierre_Id" });
            DropTable("dbo.Cierre");
            DropTable("dbo.Cierre_Detalle");
        }
    }
}
