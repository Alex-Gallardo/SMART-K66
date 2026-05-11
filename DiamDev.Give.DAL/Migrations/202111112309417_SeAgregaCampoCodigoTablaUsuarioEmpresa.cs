namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoCodigoTablaUsuarioEmpresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuario_Empresa", "Codigo", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Usuario_Empresa", "Codigo");
        }
    }
}
