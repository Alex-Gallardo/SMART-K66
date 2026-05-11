namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoFechaHoraTablasGastosReserva : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gasto", "Fecha_Hora_Gasto", c => c.DateTime());
            AddColumn("dbo.Reserva", "Fecha_Hora_Reserva", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reserva", "Fecha_Hora_Reserva");
            DropColumn("dbo.Gasto", "Fecha_Hora_Gasto");
        }
    }
}
