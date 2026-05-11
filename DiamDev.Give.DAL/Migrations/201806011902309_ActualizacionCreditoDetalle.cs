namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizacionCreditoDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Credito_Detalle", "Unidad_Id", c => c.Long(nullable: false));
            AddColumn("dbo.Credito_Detalle", "Descuento", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Credito_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Credito_Detalle", "Cantidad", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            CreateIndex("dbo.Credito_Detalle", "Unidad_Id");
            AddForeignKey("dbo.Credito_Detalle", "Unidad_Id", "dbo.Unidad", "Unidad_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Credito_Detalle", "Unidad_Id", "dbo.Unidad");
            DropIndex("dbo.Credito_Detalle", new[] { "Unidad_Id" });
            AlterColumn("dbo.Credito_Detalle", "Cantidad", c => c.Int(nullable: false));
            DropColumn("dbo.Credito_Detalle", "Precio_Costo");
            DropColumn("dbo.Credito_Detalle", "Descuento");
            DropColumn("dbo.Credito_Detalle", "Unidad_Id");
        }
    }
}
