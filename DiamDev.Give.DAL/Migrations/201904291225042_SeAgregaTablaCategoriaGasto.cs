namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaCategoriaGasto : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categoria_Gasto",
                c => new
                    {
                        Categoria_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 200),
                        Descripcion = c.String(maxLength: 500),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Categoria_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Categoria_Gasto");
        }
    }
}
