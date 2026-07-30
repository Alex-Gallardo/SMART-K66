namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoMotivoTablaFacturaNotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Nota_Credito", "Motivo", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura_Nota_Credito", "Motivo");
        }
    }
}
