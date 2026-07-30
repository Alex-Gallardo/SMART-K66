namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposTablaAgencia : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Agencia", "Codigo_Establecimiento", c => c.Long());
            AddColumn("dbo.Agencia", "Direccion", c => c.String());
            AddColumn("dbo.Agencia", "EsDelivery_Domicilio", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Agencia", "EsDelivery_Domicilio");
            DropColumn("dbo.Agencia", "Direccion");
            DropColumn("dbo.Agencia", "Codigo_Establecimiento");
        }
    }
}
