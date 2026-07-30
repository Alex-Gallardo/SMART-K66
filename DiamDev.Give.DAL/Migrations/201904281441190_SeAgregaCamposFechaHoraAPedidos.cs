namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposFechaHoraAPedidos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido", "Fecha_Hora_Opero", c => c.DateTime());
            AddColumn("dbo.Pedido", "Fecha_Hora_Creacion", c => c.DateTime());
            DropColumn("dbo.Pedido", "Fecha_Opero");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Pedido", "Fecha_Opero", c => c.DateTime());
            DropColumn("dbo.Pedido", "Fecha_Hora_Creacion");
            DropColumn("dbo.Pedido", "Fecha_Hora_Opero");
        }
    }
}
