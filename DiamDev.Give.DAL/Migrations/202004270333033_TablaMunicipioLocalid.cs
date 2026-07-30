namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TablaMunicipioLocalid : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Localidad",
                c => new
                    {
                        Localidad_Id = c.Long(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(nullable: false),
                        CostoEnvio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Activo = c.Boolean(nullable: false),
                        Municipio_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Localidad_Id)
                .ForeignKey("dbo.Municipio", t => t.Municipio_Id, cascadeDelete: true)
                .Index(t => t.Municipio_Id);
            
            CreateTable(
                "dbo.Municipio",
                c => new
                    {
                        Municipio_Id = c.Long(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(nullable: false),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Municipio_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Localidad", "Municipio_Id", "dbo.Municipio");
            DropIndex("dbo.Localidad", new[] { "Municipio_Id" });
            DropTable("dbo.Municipio");
            DropTable("dbo.Localidad");
        }
    }
}
