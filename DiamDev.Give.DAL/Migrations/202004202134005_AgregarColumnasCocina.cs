namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarColumnasCocina : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Fecha_Hora_CocinaFin", c => c.DateTime());
            AddColumn("dbo.Recibo", "Usr_Cocina", c => c.Long());
            CreateIndex("dbo.Recibo", "Usr_Cocina");
            AddForeignKey("dbo.Recibo", "Usr_Cocina", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recibo", "Usr_Cocina", "dbo.Usuario");
            DropIndex("dbo.Recibo", new[] { "Usr_Cocina" });
            DropColumn("dbo.Recibo", "Usr_Cocina");
            DropColumn("dbo.Recibo", "Fecha_Hora_CocinaFin");
        }
    }
}
