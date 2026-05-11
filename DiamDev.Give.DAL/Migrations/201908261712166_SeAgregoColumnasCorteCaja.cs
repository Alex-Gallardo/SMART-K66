namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnasCorteCaja : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Corte_Caja", "Recibido", c => c.Boolean(nullable: false));
            AddColumn("dbo.Corte_Caja", "Fecha_Hora_Recibido", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Corte_Caja", "Fecha_Hora_Recibido");
            DropColumn("dbo.Corte_Caja", "Recibido");
        }
    }
}
