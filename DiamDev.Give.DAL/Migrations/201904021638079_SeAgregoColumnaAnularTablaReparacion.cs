namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaAnularTablaReparacion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reparacion", "Usr_Anular", c => c.Long());
            AddColumn("dbo.Reparacion", "Fecha_Anular", c => c.DateTime());
            AddColumn("dbo.Reparacion", "Anulada", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Reparacion", "Usr_Anular");
            AddForeignKey("dbo.Reparacion", "Usr_Anular", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reparacion", "Usr_Anular", "dbo.Usuario");
            DropIndex("dbo.Reparacion", new[] { "Usr_Anular" });
            DropColumn("dbo.Reparacion", "Anulada");
            DropColumn("dbo.Reparacion", "Fecha_Anular");
            DropColumn("dbo.Reparacion", "Usr_Anular");
        }
    }
}
