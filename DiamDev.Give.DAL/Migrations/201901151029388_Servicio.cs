namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Servicio : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Servicio",
                c => new
                    {
                        Servicio_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 250),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Servicio_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Servicio");
        }
    }
}
