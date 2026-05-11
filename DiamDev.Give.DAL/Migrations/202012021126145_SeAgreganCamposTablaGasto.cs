namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposTablaGasto : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tipo_Compra",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
            AddColumn("dbo.Gasto", "Proveedor_Id", c => c.Long());
            AddColumn("dbo.Gasto", "Tipo_Compra_Id", c => c.Long());
            AddColumn("dbo.Gasto", "Serie_Factura", c => c.String(maxLength: 150));
            AddColumn("dbo.Gasto", "IDP", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Gasto", "Fecha_Libro", c => c.DateTime());
            CreateIndex("dbo.Gasto", "Proveedor_Id");
            CreateIndex("dbo.Gasto", "Tipo_Compra_Id");
            AddForeignKey("dbo.Gasto", "Proveedor_Id", "dbo.Proveedor", "Proveedor_Id");
            AddForeignKey("dbo.Gasto", "Tipo_Compra_Id", "dbo.Tipo_Compra", "Tipo_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Gasto", "Tipo_Compra_Id", "dbo.Tipo_Compra");
            DropForeignKey("dbo.Gasto", "Proveedor_Id", "dbo.Proveedor");
            DropIndex("dbo.Gasto", new[] { "Tipo_Compra_Id" });
            DropIndex("dbo.Gasto", new[] { "Proveedor_Id" });
            DropColumn("dbo.Gasto", "Fecha_Libro");
            DropColumn("dbo.Gasto", "IDP");
            DropColumn("dbo.Gasto", "Serie_Factura");
            DropColumn("dbo.Gasto", "Tipo_Compra_Id");
            DropColumn("dbo.Gasto", "Proveedor_Id");
            DropTable("dbo.Tipo_Compra");
        }
    }
}
