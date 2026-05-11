namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoAgenciaTablaGasto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gasto", "Agencia_Id", c => c.Long());
            CreateIndex("dbo.Gasto", "Agencia_Id");
            AddForeignKey("dbo.Gasto", "Agencia_Id", "dbo.Agencia", "Agencia_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Gasto", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Gasto", new[] { "Agencia_Id" });
            DropColumn("dbo.Gasto", "Agencia_Id");
        }
    }
}
