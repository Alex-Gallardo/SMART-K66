namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnasTablaFactura : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Factura", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Factura", new[] { "Vendedor_Id" });
            AddColumn("dbo.Factura", "Infile", c => c.Boolean(nullable: false));
            AddColumn("dbo.Factura", "Cantidad_Errores_FEL", c => c.Int(nullable: false));
            AddColumn("dbo.Factura", "Descripcion_FEL", c => c.String());
            AddColumn("dbo.Factura", "Fecha_Hora_Certificacion_FEL", c => c.String());
            AddColumn("dbo.Factura", "Numero_FEL", c => c.String());
            AddColumn("dbo.Factura", "Serie_FEL", c => c.String());
            AddColumn("dbo.Factura", "UUID_FEL", c => c.String());
            AddColumn("dbo.Factura", "XML_Certificado_FEL", c => c.String());
            AddColumn("dbo.Factura", "Json_FEL", c => c.String());
            AddColumn("dbo.Factura", "Descripcion_Anular_FEL", c => c.String());
            AddColumn("dbo.Factura", "Fecha_Hora_Certificacion_Anular_FEL", c => c.String());
            AddColumn("dbo.Factura", "XML_Certificado_Anular_FEL", c => c.String());
            AddColumn("dbo.Factura", "Json_Anular_FEL", c => c.String());
            AlterColumn("dbo.Factura", "Vendedor_Id", c => c.Long());
            CreateIndex("dbo.Factura", "Vendedor_Id");
            AddForeignKey("dbo.Factura", "Vendedor_Id", "dbo.Vendedor", "Vendedor_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Factura", new[] { "Vendedor_Id" });
            AlterColumn("dbo.Factura", "Vendedor_Id", c => c.Long(nullable: false));
            DropColumn("dbo.Factura", "Json_Anular_FEL");
            DropColumn("dbo.Factura", "XML_Certificado_Anular_FEL");
            DropColumn("dbo.Factura", "Fecha_Hora_Certificacion_Anular_FEL");
            DropColumn("dbo.Factura", "Descripcion_Anular_FEL");
            DropColumn("dbo.Factura", "Json_FEL");
            DropColumn("dbo.Factura", "XML_Certificado_FEL");
            DropColumn("dbo.Factura", "UUID_FEL");
            DropColumn("dbo.Factura", "Serie_FEL");
            DropColumn("dbo.Factura", "Numero_FEL");
            DropColumn("dbo.Factura", "Fecha_Hora_Certificacion_FEL");
            DropColumn("dbo.Factura", "Descripcion_FEL");
            DropColumn("dbo.Factura", "Cantidad_Errores_FEL");
            DropColumn("dbo.Factura", "Infile");
            CreateIndex("dbo.Factura", "Vendedor_Id");
            AddForeignKey("dbo.Factura", "Vendedor_Id", "dbo.Vendedor", "Vendedor_Id", cascadeDelete: true);
        }
    }
}
