namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaIDEnTablaFacturaDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Detalle", "ID", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura_Detalle", "ID");
        }
    }
}
