namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCambioEstructuraFacturaFormaPago : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Forma_Pago", "Fecha", c => c.DateTime(nullable: false));
            AddColumn("dbo.Factura_Forma_Pago", "Usr_Operacion_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.Factura_Forma_Pago", "Usr_Operacion_Id");
            AddForeignKey("dbo.Factura_Forma_Pago", "Usr_Operacion_Id", "dbo.Usuario", "Usuario_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Factura_Forma_Pago", "Usr_Operacion_Id", "dbo.Usuario");
            DropIndex("dbo.Factura_Forma_Pago", new[] { "Usr_Operacion_Id" });
            DropColumn("dbo.Factura_Forma_Pago", "Usr_Operacion_Id");
            DropColumn("dbo.Factura_Forma_Pago", "Fecha");
        }
    }
}
