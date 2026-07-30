namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPedidoTipoK66 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Pedido_Tipo_K66",
                c => new
                    {
                        Tipo_Id = c.Guid(nullable: false, identity: true),
                        Empresa_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Descripcion = c.String(),
                        Codigo_Intregracion_1 = c.String(),
                        Codigo_Intregracion_2 = c.String(),
                        Responsable_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_Tipo_K66", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Pedido_Tipo_K66", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Pedido_Tipo_K66", new[] { "Responsable_Id" });
            DropIndex("dbo.Pedido_Tipo_K66", new[] { "Empresa_Id" });
            DropTable("dbo.Pedido_Tipo_K66");
        }
    }
}
