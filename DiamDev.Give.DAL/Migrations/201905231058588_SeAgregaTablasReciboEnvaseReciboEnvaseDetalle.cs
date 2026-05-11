namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablasReciboEnvaseReciboEnvaseDetalle : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Recibo_Envase_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Recibo_Envase_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Cantidad_Envase = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Recibo_Envase_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Recibo_Envase", t => t.Recibo_Envase_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id)
                .Index(t => t.Recibo_Envase_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Recibo_Envase",
                c => new
                    {
                        Recibo_Envase_Id = c.Long(nullable: false),
                        Recibo_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Recibe = c.Long(),
                        Fecha_Recibe = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Recibo_Envase_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Usuario", t => t.Usr_Recibe)
                .Index(t => t.Recibo_Id)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Recibe);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo_Envase_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Recibo_Envase", "Usr_Recibe", "dbo.Usuario");
            DropForeignKey("dbo.Recibo_Envase", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Recibo_Envase", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Recibo_Envase_Detalle", "Recibo_Envase_Id", "dbo.Recibo_Envase");
            DropForeignKey("dbo.Recibo_Envase", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Recibo_Envase_Detalle", "Producto_Id", "dbo.Producto");
            DropIndex("dbo.Recibo_Envase", new[] { "Usr_Recibe" });
            DropIndex("dbo.Recibo_Envase", new[] { "Usr_Creo" });
            DropIndex("dbo.Recibo_Envase", new[] { "Agencia_Id" });
            DropIndex("dbo.Recibo_Envase", new[] { "Recibo_Id" });
            DropIndex("dbo.Recibo_Envase_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Recibo_Envase_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Recibo_Envase_Detalle", new[] { "Recibo_Envase_Id" });
            DropTable("dbo.Recibo_Envase");
            DropTable("dbo.Recibo_Envase_Detalle");
        }
    }
}
