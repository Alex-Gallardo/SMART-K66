namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizacionModuloProveedores : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Banco",
                c => new
                    {
                        Banco_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Banco_Id);
            
            CreateTable(
                "dbo.Proveedor_Cuenta_Bancaria",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Proveedor_Id = c.Long(nullable: false),
                        Banco_Id = c.Long(nullable: false),
                        Cuenta = c.String(maxLength: 150),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Proveedor_Id })
                .ForeignKey("dbo.Banco", t => t.Banco_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Banco_Id);
            
            CreateTable(
                "dbo.Proveedor_Movimiento",
                c => new
                    {
                        Movimiento_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Proveedor_Id = c.Long(nullable: false),
                        Documento = c.String(maxLength: 150),
                        Fecha_Movimiento = c.DateTime(nullable: false),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Anulada = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Anular = c.Long(),
                        Fecha_Anular = c.DateTime(),
                        Comentario = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Movimiento_Id)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor_Movimiento_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anular)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .Index(t => t.Tipo_Id)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Anular);
            
            CreateTable(
                "dbo.Proveedor_Movimiento_Fotografia",
                c => new
                    {
                        Fotografia_Id = c.Int(nullable: false),
                        Movimiento_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 200),
                        ContentType = c.String(maxLength: 150),
                        Length = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => new { t.Fotografia_Id, t.Movimiento_Id })
                .ForeignKey("dbo.Proveedor_Movimiento", t => t.Movimiento_Id, cascadeDelete: true)
                .Index(t => t.Movimiento_Id);
            
            CreateTable(
                "dbo.Proveedor_Movimiento_Tipo",
                c => new
                    {
                        Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 250),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
            AddColumn("dbo.Proveedor", "Credito", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Proveedor", "Abono", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proveedor_Movimiento", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Proveedor_Movimiento", "Usr_Anular", "dbo.Usuario");
            DropForeignKey("dbo.Proveedor_Movimiento", "Tipo_Id", "dbo.Proveedor_Movimiento_Tipo");
            DropForeignKey("dbo.Proveedor_Movimiento", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Proveedor_Movimiento_Fotografia", "Movimiento_Id", "dbo.Proveedor_Movimiento");
            DropForeignKey("dbo.Proveedor_Cuenta_Bancaria", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Proveedor_Cuenta_Bancaria", "Banco_Id", "dbo.Banco");
            DropIndex("dbo.Proveedor_Movimiento_Fotografia", new[] { "Movimiento_Id" });
            DropIndex("dbo.Proveedor_Movimiento", new[] { "Usr_Anular" });
            DropIndex("dbo.Proveedor_Movimiento", new[] { "Usr_Creo" });
            DropIndex("dbo.Proveedor_Movimiento", new[] { "Proveedor_Id" });
            DropIndex("dbo.Proveedor_Movimiento", new[] { "Tipo_Id" });
            DropIndex("dbo.Proveedor_Cuenta_Bancaria", new[] { "Banco_Id" });
            DropIndex("dbo.Proveedor_Cuenta_Bancaria", new[] { "Proveedor_Id" });
            DropColumn("dbo.Proveedor", "Abono");
            DropColumn("dbo.Proveedor", "Credito");
            DropTable("dbo.Proveedor_Movimiento_Tipo");
            DropTable("dbo.Proveedor_Movimiento_Fotografia");
            DropTable("dbo.Proveedor_Movimiento");
            DropTable("dbo.Proveedor_Cuenta_Bancaria");
            DropTable("dbo.Banco");
        }
    }
}
