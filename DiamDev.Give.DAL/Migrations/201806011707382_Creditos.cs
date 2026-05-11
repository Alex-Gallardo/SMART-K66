namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Creditos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Credito_Anotacion",
                c => new
                    {
                        Anotacion_Id = c.Int(nullable: false),
                        Credito_Id = c.Long(nullable: false),
                        Comentario = c.String(nullable: false),
                        Fecha_Anotacion = c.DateTime(nullable: false),
                        Usr_Anotacion = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Anotacion_Id, t.Credito_Id })
                .ForeignKey("dbo.Credito", t => t.Credito_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anotacion, cascadeDelete: true)
                .Index(t => t.Credito_Id)
                .Index(t => t.Usr_Anotacion);
            
            CreateTable(
                "dbo.Credito",
                c => new
                    {
                        Credito_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(),
                        Serie = c.String(maxLength: 50),
                        Factura = c.String(maxLength: 50),
                        Descripcion = c.String(nullable: false),
                        Fecha_Inicial = c.DateTime(nullable: false),
                        Fecha_Final = c.DateTime(nullable: false),
                        Finalizado = c.Boolean(nullable: false),
                        Usr_Inicial = c.Long(nullable: false),
                        Usr_Final = c.Long(),
                        Fecha_Cancelacion = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Credito_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id)
                .ForeignKey("dbo.Credito_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Final)
                .ForeignKey("dbo.Usuario", t => t.Usr_Inicial)
                .Index(t => t.Tipo_Id)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Usr_Inicial)
                .Index(t => t.Usr_Final);
            
            CreateTable(
                "dbo.Credito_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Credito_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Cantidad = c.Int(nullable: false),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Credito_Id })
                .ForeignKey("dbo.Credito", t => t.Credito_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .Index(t => t.Credito_Id)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Credito_Pago",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Credito_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Usr_Operacion_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Credito_Id })
                .ForeignKey("dbo.Credito", t => t.Credito_Id, cascadeDelete: true)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Operacion_Id, cascadeDelete: true)
                .Index(t => t.Credito_Id)
                .Index(t => t.Forma_Pago_Id)
                .Index(t => t.Usr_Operacion_Id);
            
            CreateTable(
                "dbo.Credito_Tipo",
                c => new
                    {
                        Credito_Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Credito_Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Credito_Anotacion", "Usr_Anotacion", "dbo.Usuario");
            DropForeignKey("dbo.Credito", "Usr_Inicial", "dbo.Usuario");
            DropForeignKey("dbo.Credito", "Usr_Final", "dbo.Usuario");
            DropForeignKey("dbo.Credito", "Tipo_Id", "dbo.Credito_Tipo");
            DropForeignKey("dbo.Credito_Pago", "Usr_Operacion_Id", "dbo.Usuario");
            DropForeignKey("dbo.Credito_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Credito_Pago", "Credito_Id", "dbo.Credito");
            DropForeignKey("dbo.Credito_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Credito_Detalle", "Credito_Id", "dbo.Credito");
            DropForeignKey("dbo.Credito_Anotacion", "Credito_Id", "dbo.Credito");
            DropForeignKey("dbo.Credito", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Credito", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Credito_Pago", new[] { "Usr_Operacion_Id" });
            DropIndex("dbo.Credito_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Credito_Pago", new[] { "Credito_Id" });
            DropIndex("dbo.Credito_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Credito_Detalle", new[] { "Credito_Id" });
            DropIndex("dbo.Credito", new[] { "Usr_Final" });
            DropIndex("dbo.Credito", new[] { "Usr_Inicial" });
            DropIndex("dbo.Credito", new[] { "Cliente_Id" });
            DropIndex("dbo.Credito", new[] { "Agencia_Id" });
            DropIndex("dbo.Credito", new[] { "Tipo_Id" });
            DropIndex("dbo.Credito_Anotacion", new[] { "Usr_Anotacion" });
            DropIndex("dbo.Credito_Anotacion", new[] { "Credito_Id" });
            DropTable("dbo.Credito_Tipo");
            DropTable("dbo.Credito_Pago");
            DropTable("dbo.Credito_Detalle");
            DropTable("dbo.Credito");
            DropTable("dbo.Credito_Anotacion");
        }
    }
}
