namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaDiasCreditoATablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Dias_Credito", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cliente", "Dias_Credito");
        }
    }
}
