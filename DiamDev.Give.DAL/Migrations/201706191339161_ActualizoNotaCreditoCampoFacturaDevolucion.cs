namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizoNotaCreditoCampoFacturaDevolucion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Nota_Credito", "Factura_Id", c => c.Long());
            AddColumn("dbo.Nota_Credito", "Devolucion", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Nota_Credito", "Factura_Id");
            AddForeignKey("dbo.Nota_Credito", "Factura_Id", "dbo.Factura", "Factura_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Nota_Credito", "Factura_Id", "dbo.Factura");
            DropIndex("dbo.Nota_Credito", new[] { "Factura_Id" });
            DropColumn("dbo.Nota_Credito", "Devolucion");
            DropColumn("dbo.Nota_Credito", "Factura_Id");
        }
    }
}
