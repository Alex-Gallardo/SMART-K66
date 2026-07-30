namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoBodegaActivaTablaEmpresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresa", "Bodega_Activa", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresa", "Bodega_Activa");
        }
    }
}
