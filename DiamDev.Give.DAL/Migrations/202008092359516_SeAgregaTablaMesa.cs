namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaMesa : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Mesa",
                c => new
                    {
                        Mesa_Id = c.Long(nullable: false),
                        Tipo_Ubicacion_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(),
                        Ocupado = c.Boolean(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Mesa_Id)
                .ForeignKey("dbo.Tipo_Ubicacion", t => t.Tipo_Ubicacion_Id, cascadeDelete: true)
                .Index(t => t.Tipo_Ubicacion_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Mesa", "Tipo_Ubicacion_Id", "dbo.Tipo_Ubicacion");
            DropIndex("dbo.Mesa", new[] { "Tipo_Ubicacion_Id" });
            DropTable("dbo.Mesa");
        }
    }
}
