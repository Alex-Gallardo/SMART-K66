namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarFacturaColumnaNotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Nota_Credito_Id", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "Nota_Credito_Id");
        }
    }
}
