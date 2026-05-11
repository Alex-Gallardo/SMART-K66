namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Reparacion : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Reparacion_Anotacion",
                c => new
                    {
                        Anotacion_Id = c.Int(nullable: false),
                        Reparacion_Id = c.Long(nullable: false),
                        Comentario = c.String(nullable: false),
                        Fecha_Anotacion = c.DateTime(nullable: false),
                        Usr_Anotacion = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Anotacion_Id, t.Reparacion_Id })
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anotacion, cascadeDelete: true)
                .Index(t => t.Reparacion_Id)
                .Index(t => t.Usr_Anotacion);
            
            CreateTable(
                "dbo.Reparacion",
                c => new
                    {
                        Reparacion_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Departamento_Id = c.Long(nullable: false),
                        Estado_Id = c.Int(nullable: false),
                        Tipo_Id = c.Int(),
                        Serie = c.String(maxLength: 50),
                        Factura = c.String(maxLength: 50),
                        Marca = c.String(),
                        Falla = c.String(),
                        IMEI = c.String(),
                        Descripcion = c.String(),
                        Garantia = c.String(),
                        Comentario = c.String(),
                        Costo_Servicio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Descuento = c.Int(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Asignado = c.Long(),
                        Usr_Entrega = c.Long(),
                        Fecha_Inicia_Reparacion = c.DateTime(),
                        Fecha_Finaliza_Reparacion = c.DateTime(),
                        Fecha_Cancelacion = c.DateTime(),
                        Fecha_Entrega = c.DateTime(nullable: false),
                        Operado = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Reparacion_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Departamento", t => t.Departamento_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion_Estado", t => t.Estado_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion_Tipo", t => t.Tipo_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Asignado)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Usuario", t => t.Usr_Entrega)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Departamento_Id)
                .Index(t => t.Estado_Id)
                .Index(t => t.Tipo_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Asignado)
                .Index(t => t.Usr_Entrega);
            
            CreateTable(
                "dbo.Reparacion_Estado",
                c => new
                    {
                        EstadoId = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 200),
                    })
                .PrimaryKey(t => t.EstadoId);
            
            CreateTable(
                "dbo.Reparacion_Fotografia",
                c => new
                    {
                        Fotografia_Id = c.Int(nullable: false),
                        Reparacion_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 200),
                        ContentType = c.String(maxLength: 150),
                        Length = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => new { t.Fotografia_Id, t.Reparacion_Id })
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .Index(t => t.Reparacion_Id);
            
            CreateTable(
                "dbo.Reparacion_Forma_Pago",
                c => new
                    {
                        Reparacion_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Reparacion_Id, t.Forma_Pago_Id })
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .Index(t => t.Reparacion_Id)
                .Index(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Reparacion_Pieza",
                c => new
                    {
                        Reparacion_Id = c.Long(nullable: false),
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Cantidad = c.Int(nullable: false),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Reparacion_Id, t.Producto_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .Index(t => t.Reparacion_Id)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Reparacion_Servicio",
                c => new
                    {
                        Reparacion_Id = c.Long(nullable: false),
                        Servicio_Id = c.Long(nullable: false),
                        Estado = c.Boolean(nullable: false),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Reparacion_Id, t.Servicio_Id })
                .ForeignKey("dbo.Servicio", t => t.Servicio_Id, cascadeDelete: true)
                .ForeignKey("dbo.Reparacion", t => t.Reparacion_Id, cascadeDelete: true)
                .Index(t => t.Reparacion_Id)
                .Index(t => t.Servicio_Id);
            
            CreateTable(
                "dbo.Reparacion_Tipo",
                c => new
                    {
                        Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reparacion_Anotacion", "Usr_Anotacion", "dbo.Usuario");
            DropForeignKey("dbo.Reparacion", "Usr_Entrega", "dbo.Usuario");
            DropForeignKey("dbo.Reparacion", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Reparacion", "Usr_Asignado", "dbo.Usuario");
            DropForeignKey("dbo.Reparacion", "Tipo_Id", "dbo.Reparacion_Tipo");
            DropForeignKey("dbo.Reparacion_Servicio", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion_Servicio", "Servicio_Id", "dbo.Servicio");
            DropForeignKey("dbo.Reparacion_Pieza", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion_Pieza", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Reparacion_Forma_Pago", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion_Forma_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Reparacion_Fotografia", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion", "Estado_Id", "dbo.Reparacion_Estado");
            DropForeignKey("dbo.Reparacion", "Departamento_Id", "dbo.Departamento");
            DropForeignKey("dbo.Reparacion_Anotacion", "Reparacion_Id", "dbo.Reparacion");
            DropForeignKey("dbo.Reparacion", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Reparacion", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Reparacion_Servicio", new[] { "Servicio_Id" });
            DropIndex("dbo.Reparacion_Servicio", new[] { "Reparacion_Id" });
            DropIndex("dbo.Reparacion_Pieza", new[] { "Producto_Id" });
            DropIndex("dbo.Reparacion_Pieza", new[] { "Reparacion_Id" });
            DropIndex("dbo.Reparacion_Forma_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Reparacion_Forma_Pago", new[] { "Reparacion_Id" });
            DropIndex("dbo.Reparacion_Fotografia", new[] { "Reparacion_Id" });
            DropIndex("dbo.Reparacion", new[] { "Usr_Entrega" });
            DropIndex("dbo.Reparacion", new[] { "Usr_Asignado" });
            DropIndex("dbo.Reparacion", new[] { "Usr_Creo" });
            DropIndex("dbo.Reparacion", new[] { "Tipo_Id" });
            DropIndex("dbo.Reparacion", new[] { "Estado_Id" });
            DropIndex("dbo.Reparacion", new[] { "Departamento_Id" });
            DropIndex("dbo.Reparacion", new[] { "Cliente_Id" });
            DropIndex("dbo.Reparacion", new[] { "Agencia_Id" });
            DropIndex("dbo.Reparacion_Anotacion", new[] { "Usr_Anotacion" });
            DropIndex("dbo.Reparacion_Anotacion", new[] { "Reparacion_Id" });
            DropTable("dbo.Reparacion_Tipo");
            DropTable("dbo.Reparacion_Servicio");
            DropTable("dbo.Reparacion_Pieza");
            DropTable("dbo.Reparacion_Forma_Pago");
            DropTable("dbo.Reparacion_Fotografia");
            DropTable("dbo.Reparacion_Estado");
            DropTable("dbo.Reparacion");
            DropTable("dbo.Reparacion_Anotacion");
        }
    }
}
