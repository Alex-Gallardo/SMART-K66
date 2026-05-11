namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgenciaEnlazadaLocalidadSI : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Localidad", "Agencia_Id", c => c.Long());
            CreateIndex("dbo.Localidad", "Agencia_Id");
            AddForeignKey("dbo.Localidad", "Agencia_Id", "dbo.Agencia", "Agencia_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Localidad", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Localidad", new[] { "Agencia_Id" });
            DropColumn("dbo.Localidad", "Agencia_Id");
        }
    }
}
