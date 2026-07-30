namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoFELTablaFacturaNotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Nota_Credito", "Numero_FEL", c => c.String());
            AddColumn("dbo.Factura_Nota_Credito", "Serie_FEL", c => c.String());
            AddColumn("dbo.Factura_Nota_Credito", "UUID_FEL", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura_Nota_Credito", "UUID_FEL");
            DropColumn("dbo.Factura_Nota_Credito", "Serie_FEL");
            DropColumn("dbo.Factura_Nota_Credito", "Numero_FEL");
        }
    }
}
