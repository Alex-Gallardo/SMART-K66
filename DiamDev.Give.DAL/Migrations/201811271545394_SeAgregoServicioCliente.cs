namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoServicioCliente : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ServicioCliente",
                c => new
                    {
                        id = c.Long(nullable: false, identity: true),
                        correlativo = c.Int(nullable: false),
                        tipo = c.Int(nullable: false),
                        fecha = c.DateTime(nullable: false),
                        atendidopor = c.String(maxLength: 300),
                        estado = c.Int(nullable: false),
                        hora_entrada = c.DateTime(nullable: false),
                        hora_atendido = c.DateTime(),
                        hora_entrega = c.DateTime(),
                        agencia_id = c.Long(nullable: false),
                        factura_id = c.Long(),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.Agencia", t => t.agencia_id, cascadeDelete: true)
                .ForeignKey("dbo.Factura", t => t.factura_id)
                .ForeignKey("dbo.TipoServicioCliente", t => t.tipo, cascadeDelete: true)
                .Index(t => t.tipo)
                .Index(t => t.agencia_id)
                .Index(t => t.factura_id);
            
            CreateTable(
                "dbo.TipoServicioCliente",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        nombretipo = c.String(maxLength: 150),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ServicioCliente", "tipo", "dbo.TipoServicioCliente");
            DropForeignKey("dbo.ServicioCliente", "factura_id", "dbo.Factura");
            DropForeignKey("dbo.ServicioCliente", "agencia_id", "dbo.Agencia");
            DropIndex("dbo.ServicioCliente", new[] { "factura_id" });
            DropIndex("dbo.ServicioCliente", new[] { "agencia_id" });
            DropIndex("dbo.ServicioCliente", new[] { "tipo" });
            DropTable("dbo.TipoServicioCliente");
            DropTable("dbo.ServicioCliente");
        }
    }
}
