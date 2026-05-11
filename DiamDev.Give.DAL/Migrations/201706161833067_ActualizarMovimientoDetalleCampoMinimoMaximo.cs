namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarMovimientoDetalleCampoMinimoMaximo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento_Detalle", "Minimo", c => c.Int(nullable: false));
            AddColumn("dbo.Movimiento_Detalle", "Maximo", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento_Detalle", "Maximo");
            DropColumn("dbo.Movimiento_Detalle", "Minimo");
        }
    }
}
