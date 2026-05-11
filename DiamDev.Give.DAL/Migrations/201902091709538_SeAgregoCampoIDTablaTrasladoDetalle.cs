namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoIDTablaTrasladoDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Traslado_Detalle", "ID", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Traslado_Detalle", "ID");
        }
    }
}
