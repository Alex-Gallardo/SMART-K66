namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaVisitaTipo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Visita_Tipo",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Visita_Tipo");
        }
    }
}
