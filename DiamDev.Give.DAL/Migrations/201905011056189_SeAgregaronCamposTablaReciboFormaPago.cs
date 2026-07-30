namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaronCamposTablaReciboFormaPago : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Recibo_Forma_Pago");
            AddColumn("dbo.Recibo_Forma_Pago", "Detalle_Id", c => c.Int(nullable: false));
            AddColumn("dbo.Recibo_Forma_Pago", "Fecha", c => c.DateTime(nullable: false));
            AddColumn("dbo.Recibo_Forma_Pago", "Usr_Operacion_Id", c => c.Long(nullable: false));
            AddPrimaryKey("dbo.Recibo_Forma_Pago", new[] { "Detalle_Id", "Recibo_Id" });
            CreateIndex("dbo.Recibo_Forma_Pago", "Usr_Operacion_Id");
            AddForeignKey("dbo.Recibo_Forma_Pago", "Usr_Operacion_Id", "dbo.Usuario", "Usuario_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo_Forma_Pago", "Usr_Operacion_Id", "dbo.Usuario");
            DropIndex("dbo.Recibo_Forma_Pago", new[] { "Usr_Operacion_Id" });
            DropPrimaryKey("dbo.Recibo_Forma_Pago");
            DropColumn("dbo.Recibo_Forma_Pago", "Usr_Operacion_Id");
            DropColumn("dbo.Recibo_Forma_Pago", "Fecha");
            DropColumn("dbo.Recibo_Forma_Pago", "Detalle_Id");
            AddPrimaryKey("dbo.Recibo_Forma_Pago", new[] { "Recibo_Id", "Forma_Pago_Id" });
        }
    }
}
