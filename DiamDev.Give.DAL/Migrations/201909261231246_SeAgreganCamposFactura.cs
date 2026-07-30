namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposFactura : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Despachado", c => c.Boolean(nullable: false));
            AddColumn("dbo.Factura", "Usr_Despacho", c => c.Long());
            AddColumn("dbo.Factura", "Fecha_Hora_Despacho", c => c.DateTime());
            CreateIndex("dbo.Factura", "Usr_Despacho");
            AddForeignKey("dbo.Factura", "Usr_Despacho", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura", "Usr_Despacho", "dbo.Usuario");
            DropIndex("dbo.Factura", new[] { "Usr_Despacho" });
            DropColumn("dbo.Factura", "Fecha_Hora_Despacho");
            DropColumn("dbo.Factura", "Usr_Despacho");
            DropColumn("dbo.Factura", "Despachado");
        }
    }
}
