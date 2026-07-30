namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoRegionIdTablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Region_Id", c => c.Long());
            CreateIndex("dbo.Cliente", "Region_Id");
            AddForeignKey("dbo.Cliente", "Region_Id", "dbo.Region", "Region_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente", "Region_Id", "dbo.Region");
            DropIndex("dbo.Cliente", new[] { "Region_Id" });
            DropColumn("dbo.Cliente", "Region_Id");
        }
    }
}
