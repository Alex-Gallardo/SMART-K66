namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoAgenciaTablaMesa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Mesa", "Agencia_Id", c => c.Long());
            CreateIndex("dbo.Mesa", "Agencia_Id");
            AddForeignKey("dbo.Mesa", "Agencia_Id", "dbo.Agencia", "Agencia_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Mesa", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Mesa", new[] { "Agencia_Id" });
            DropColumn("dbo.Mesa", "Agencia_Id");
        }
    }
}
