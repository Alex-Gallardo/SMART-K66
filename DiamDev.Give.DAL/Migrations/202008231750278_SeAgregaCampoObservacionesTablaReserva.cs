namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoObservacionesTablaReserva : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reserva", "Observaciones", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reserva", "Observaciones");
        }
    }
}
