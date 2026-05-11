namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoNombreDBTablaEmpresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresa", "Nombre_DB", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresa", "Nombre_DB");
        }
    }
}
