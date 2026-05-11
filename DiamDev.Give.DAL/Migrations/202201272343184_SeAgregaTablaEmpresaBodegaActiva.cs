namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaEmpresaBodegaActiva : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Empresa_Bodega_Activa",
                c => new
                    {
                        Bodega_Id = c.Guid(nullable: false, identity: true),
                        Empresa_Id = c.Long(nullable: false),
                        Warehouse_Id = c.String(),
                        Location_Id = c.String(),
                    })
                .PrimaryKey(t => t.Bodega_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Empresa_Bodega_Activa", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Empresa_Bodega_Activa", new[] { "Empresa_Id" });
            DropTable("dbo.Empresa_Bodega_Activa");
        }
    }
}
