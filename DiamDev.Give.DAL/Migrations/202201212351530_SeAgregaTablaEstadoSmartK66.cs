namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaEstadoSmartK66 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Estado_Smart_K66",
                c => new
                    {
                        Estado_Id = c.Int(nullable: false),
                        Nombre = c.String(),
                        Descripcion = c.String(),
                    })
                .PrimaryKey(t => t.Estado_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Estado_Smart_K66");
        }
    }
}
