namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaTipoUbicacion : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tipo_Ubicacion",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Tipo_Ubicacion");
        }
    }
}
