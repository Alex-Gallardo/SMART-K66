namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCreoTablaRecibos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Recibo_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Recibo_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Descuento = c.Decimal(precision: 18, scale: 2),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ID = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Recibo_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id)
                .Index(t => t.Recibo_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Recibo",
                c => new
                    {
                        Recibo_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(),
                        Agencia_Id = c.Long(nullable: false),
                        Vendedor_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Comentario = c.String(),
                        Descuento = c.Int(nullable: false),
                        Anulada = c.Boolean(nullable: false),
                        Empleado = c.Boolean(nullable: false),
                        Reparto = c.Boolean(nullable: false),
                        Pagada = c.Boolean(nullable: false),
                        Transporte_Id = c.Long(),
                        Entregado_Transporte = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Anular = c.Long(),
                        Fecha_Anular = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                        Fecha_Hora_Recibo = c.DateTime(),
                        Credito = c.Boolean(nullable: false),
                        Dia_Credito = c.Int(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Recibo_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Recibo_Tipo", t => t.Tipo_Id)
                .ForeignKey("dbo.Transporte", t => t.Transporte_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anular)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Tipo_Id)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Vendedor_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Transporte_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Anular);
            
            CreateTable(
                "dbo.Recibo_Forma_Pago",
                c => new
                    {
                        Recibo_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Recibo_Id, t.Forma_Pago_Id })
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id, cascadeDelete: true)
                .Index(t => t.Recibo_Id)
                .Index(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Recibo_Tipo",
                c => new
                    {
                        Recibo_Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Recibo_Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Recibo", "Vendedor_Id", "dbo.Vendedor");
            DropForeignKey("dbo.Recibo", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Recibo", "Usr_Anular", "dbo.Usuario");
            DropForeignKey("dbo.Recibo", "Transporte_Id", "dbo.Transporte");
            DropForeignKey("dbo.Recibo", "Tipo_Id", "dbo.Recibo_Tipo");
            DropForeignKey("dbo.Recibo_Forma_Pago", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Recibo_Forma_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Recibo_Detalle", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Recibo", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Recibo", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Recibo_Detalle", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Recibo_Forma_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Recibo_Forma_Pago", new[] { "Recibo_Id" });
            DropIndex("dbo.Recibo", new[] { "Usr_Anular" });
            DropIndex("dbo.Recibo", new[] { "Usr_Creo" });
            DropIndex("dbo.Recibo", new[] { "Transporte_Id" });
            DropIndex("dbo.Recibo", new[] { "Cliente_Id" });
            DropIndex("dbo.Recibo", new[] { "Vendedor_Id" });
            DropIndex("dbo.Recibo", new[] { "Agencia_Id" });
            DropIndex("dbo.Recibo", new[] { "Tipo_Id" });
            DropIndex("dbo.Recibo_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Recibo_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Recibo_Detalle", new[] { "Recibo_Id" });
            DropTable("dbo.Recibo_Tipo");
            DropTable("dbo.Recibo_Forma_Pago");
            DropTable("dbo.Recibo");
            DropTable("dbo.Recibo_Detalle");
        }
    }
}
