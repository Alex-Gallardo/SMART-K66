namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaToken : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Token",
                c => new
                    {
                        Token_Id = c.Long(nullable: false),
                        Token_Valido = c.String(),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Token_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Token");
        }
    }
}
