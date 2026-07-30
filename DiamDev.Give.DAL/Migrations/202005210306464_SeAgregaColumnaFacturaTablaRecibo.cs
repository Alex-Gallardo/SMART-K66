namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaFacturaTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Factura", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Factura");
        }
    }
}
