namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaRelacionDieccionyloclidad : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DireccionCliente", "Localidad_Id", c => c.Long());
            CreateIndex("dbo.DireccionCliente", "Localidad_Id");
            AddForeignKey("dbo.DireccionCliente", "Localidad_Id", "dbo.Localidad", "Localidad_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DireccionCliente", "Localidad_Id", "dbo.Localidad");
            DropIndex("dbo.DireccionCliente", new[] { "Localidad_Id" });
            DropColumn("dbo.DireccionCliente", "Localidad_Id");
        }
    }
}
