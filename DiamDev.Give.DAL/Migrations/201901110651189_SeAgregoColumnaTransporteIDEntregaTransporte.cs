namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaTransporteIDEntregaTransporte : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Transporte_Id", c => c.Long());
            AddColumn("dbo.Factura", "Entregado_Transporte", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Factura", "Transporte_Id");
            AddForeignKey("dbo.Factura", "Transporte_Id", "dbo.Transporte", "Transporte_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura", "Transporte_Id", "dbo.Transporte");
            DropIndex("dbo.Factura", new[] { "Transporte_Id" });
            DropColumn("dbo.Factura", "Entregado_Transporte");
            DropColumn("dbo.Factura", "Transporte_Id");
        }
    }
}
