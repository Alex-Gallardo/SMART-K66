namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCamposAnulacionTablaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Credito", "Comentario", c => c.String());
            AddColumn("dbo.Credito", "Anulada", c => c.Boolean(nullable: false));
            AddColumn("dbo.Credito", "Usr_Anular", c => c.Long());
            AddColumn("dbo.Credito", "Fecha_Anular", c => c.DateTime());
            CreateIndex("dbo.Credito", "Usr_Anular");
            AddForeignKey("dbo.Credito", "Usr_Anular", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Credito", "Usr_Anular", "dbo.Usuario");
            DropIndex("dbo.Credito", new[] { "Usr_Anular" });
            DropColumn("dbo.Credito", "Fecha_Anular");
            DropColumn("dbo.Credito", "Usr_Anular");
            DropColumn("dbo.Credito", "Anulada");
            DropColumn("dbo.Credito", "Comentario");
        }
    }
}
