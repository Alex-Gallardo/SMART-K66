namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCamposDespachoTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Usr_Despacho", c => c.Long());
            AddColumn("dbo.Recibo", "Fecha_Hora_Despacho", c => c.DateTime());
            CreateIndex("dbo.Recibo", "Usr_Despacho");
            AddForeignKey("dbo.Recibo", "Usr_Despacho", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo", "Usr_Despacho", "dbo.Usuario");
            DropIndex("dbo.Recibo", new[] { "Usr_Despacho" });
            DropColumn("dbo.Recibo", "Fecha_Hora_Despacho");
            DropColumn("dbo.Recibo", "Usr_Despacho");
        }
    }
}
