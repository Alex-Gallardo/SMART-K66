namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCamoDireccioaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "DireccionCliente", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "DireccionCliente");
        }
    }
}
