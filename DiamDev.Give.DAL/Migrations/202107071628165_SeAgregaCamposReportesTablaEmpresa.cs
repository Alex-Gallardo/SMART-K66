namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposReportesTablaEmpresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresa", "Reporte_1", c => c.String());
            AddColumn("dbo.Empresa", "Reporte_2", c => c.String());
            AddColumn("dbo.Empresa", "Reporte_Cotizacion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresa", "Reporte_Cotizacion");
            DropColumn("dbo.Empresa", "Reporte_2");
            DropColumn("dbo.Empresa", "Reporte_1");
        }
    }
}
