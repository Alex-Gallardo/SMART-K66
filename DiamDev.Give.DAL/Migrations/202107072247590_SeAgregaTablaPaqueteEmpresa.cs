namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPaqueteEmpresa : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Paquete_Empresa",
                c => new
                    {
                        Paquete_Empresa_Id = c.Long(nullable: false),
                        Empresa_Id = c.Long(nullable: false),
                        Paquete_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Saldo_Factura = c.Int(nullable: false),
                        Fecha_Vencimiento = c.DateTime(nullable: false),
                        Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Paquete_Empresa_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Paquete", t => t.Paquete_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Paquete_Id)
                .Index(t => t.Forma_Pago_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Paquete_Empresa", "Paquete_Id", "dbo.Paquete");
            DropForeignKey("dbo.Paquete_Empresa", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Paquete_Empresa", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Paquete_Empresa", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Paquete_Empresa", new[] { "Paquete_Id" });
            DropIndex("dbo.Paquete_Empresa", new[] { "Empresa_Id" });
            DropTable("dbo.Paquete_Empresa");
        }
    }
}
