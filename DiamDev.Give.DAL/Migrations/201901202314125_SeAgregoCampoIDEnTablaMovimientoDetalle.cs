namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoIDEnTablaMovimientoDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento_Detalle", "ID", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento_Detalle", "ID");
        }
    }
}
