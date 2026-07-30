namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CamposProgramacionPedidosRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Fecha_Hora_Entrega_Programada", c => c.DateTime());
            AddColumn("dbo.Recibo", "Programada", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Programada");
            DropColumn("dbo.Recibo", "Fecha_Hora_Entrega_Programada");
        }
    }
}
