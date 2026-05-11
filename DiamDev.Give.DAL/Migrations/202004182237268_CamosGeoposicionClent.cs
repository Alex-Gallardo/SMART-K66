namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CamosGeoposicionClent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Latitud", c => c.String(maxLength: 100));
            AddColumn("dbo.Cliente", "Longitud", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cliente", "Longitud");
            DropColumn("dbo.Cliente", "Latitud");
        }
    }
}
