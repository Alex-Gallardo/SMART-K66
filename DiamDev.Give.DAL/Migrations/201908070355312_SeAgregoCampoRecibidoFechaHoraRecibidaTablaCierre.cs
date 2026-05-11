namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoRecibidoFechaHoraRecibidaTablaCierre : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cierre", "Recibido", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cierre", "Fecha_Hora_Recibido", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cierre", "Fecha_Hora_Recibido");
            DropColumn("dbo.Cierre", "Recibido");
        }
    }
}
