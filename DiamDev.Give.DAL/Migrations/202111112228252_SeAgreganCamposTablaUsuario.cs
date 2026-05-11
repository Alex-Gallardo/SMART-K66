namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposTablaUsuario : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuario", "Token", c => c.Boolean(nullable: false));
            AddColumn("dbo.Usuario", "Celular", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Usuario", "Celular");
            DropColumn("dbo.Usuario", "Token");
        }
    }
}
