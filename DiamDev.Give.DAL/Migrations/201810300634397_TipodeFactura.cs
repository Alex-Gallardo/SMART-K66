namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TipodeFactura : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Factura_Tipo",
                c => new
                    {
                        Factura_Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Factura_Tipo_Id);
            
            AddColumn("dbo.Factura", "Tipo_Id", c => c.Int());
            CreateIndex("dbo.Factura", "Tipo_Id");
            AddForeignKey("dbo.Factura", "Tipo_Id", "dbo.Factura_Tipo", "Factura_Tipo_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura", "Tipo_Id", "dbo.Factura_Tipo");
            DropIndex("dbo.Factura", new[] { "Tipo_Id" });
            DropColumn("dbo.Factura", "Tipo_Id");
            DropTable("dbo.Factura_Tipo");
        }
    }
}
