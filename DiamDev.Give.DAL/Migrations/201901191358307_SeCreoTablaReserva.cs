namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCreoTablaReserva : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Reserva_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Reserva_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Reserva_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Reserva", t => t.Reserva_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Reserva_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Reserva",
                c => new
                    {
                        Reserva_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Telefono = c.String(maxLength: 15),
                        Operado = c.Boolean(nullable: false),
                        Anulada = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Anular = c.Long(),
                        Fecha_Anular = c.DateTime(),
                        Comentario = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Reserva_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anular)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Anular);
            
            CreateTable(
                "dbo.Reserva_Pago",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Reserva_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Usr_Operacion_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Reserva_Id })
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reserva", t => t.Reserva_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Operacion_Id, cascadeDelete: true)
                .Index(t => t.Reserva_Id)
                .Index(t => t.Forma_Pago_Id)
                .Index(t => t.Usr_Operacion_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reserva_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Reserva", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Reserva", "Usr_Anular", "dbo.Usuario");
            DropForeignKey("dbo.Reserva_Pago", "Usr_Operacion_Id", "dbo.Usuario");
            DropForeignKey("dbo.Reserva_Pago", "Reserva_Id", "dbo.Reserva");
            DropForeignKey("dbo.Reserva_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Reserva_Detalle", "Reserva_Id", "dbo.Reserva");
            DropForeignKey("dbo.Reserva", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Reserva", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Reserva_Detalle", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Reserva_Pago", new[] { "Usr_Operacion_Id" });
            DropIndex("dbo.Reserva_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Reserva_Pago", new[] { "Reserva_Id" });
            DropIndex("dbo.Reserva", new[] { "Usr_Anular" });
            DropIndex("dbo.Reserva", new[] { "Usr_Creo" });
            DropIndex("dbo.Reserva", new[] { "Cliente_Id" });
            DropIndex("dbo.Reserva", new[] { "Agencia_Id" });
            DropIndex("dbo.Reserva_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Reserva_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Reserva_Detalle", new[] { "Reserva_Id" });
            DropTable("dbo.Reserva_Pago");
            DropTable("dbo.Reserva");
            DropTable("dbo.Reserva_Detalle");
        }
    }
}
