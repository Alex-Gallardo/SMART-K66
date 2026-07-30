namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoUsuarioDespachoFechaHoraTablaTraslado : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Traslado", "Usr_Despacho", c => c.Long());
            AddColumn("dbo.Traslado", "Fecha_Hora_Despacho", c => c.DateTime());
            CreateIndex("dbo.Traslado", "Usr_Despacho");
            AddForeignKey("dbo.Traslado", "Usr_Despacho", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Traslado", "Usr_Despacho", "dbo.Usuario");
            DropIndex("dbo.Traslado", new[] { "Usr_Despacho" });
            DropColumn("dbo.Traslado", "Fecha_Hora_Despacho");
            DropColumn("dbo.Traslado", "Usr_Despacho");
        }
    }
}
