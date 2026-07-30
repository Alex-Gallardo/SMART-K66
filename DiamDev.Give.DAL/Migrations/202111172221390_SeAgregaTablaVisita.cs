namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaVisita : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Visita",
                c => new
                    {
                        Visita_Id = c.Long(nullable: false),
                        Empresa_Id = c.Long(nullable: false),
                        Tipo_Visita_Id = c.Long(nullable: false),
                        ID_K66 = c.String(),
                        Nit = c.String(),
                        Nombre = c.String(),
                        Direccion = c.String(),
                        Observaciones = c.String(),
                        Latitud = c.String(),
                        Longitud = c.String(),
                        Responsable_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Visita_Id)
                .ForeignKey("dbo.Empresa", t => t.Empresa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .ForeignKey("dbo.Visita_Tipo", t => t.Tipo_Visita_Id, cascadeDelete: true)
                .Index(t => t.Empresa_Id)
                .Index(t => t.Tipo_Visita_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Visita", "Tipo_Visita_Id", "dbo.Visita_Tipo");
            DropForeignKey("dbo.Visita", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Visita", "Empresa_Id", "dbo.Empresa");
            DropIndex("dbo.Visita", new[] { "Responsable_Id" });
            DropIndex("dbo.Visita", new[] { "Tipo_Visita_Id" });
            DropIndex("dbo.Visita", new[] { "Empresa_Id" });
            DropTable("dbo.Visita");
        }
    }
}
