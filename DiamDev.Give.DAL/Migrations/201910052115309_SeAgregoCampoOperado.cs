namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoOperado : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Contrasena_Pago", "Operado", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Contrasena_Pago", "Operado");
        }
    }
}
