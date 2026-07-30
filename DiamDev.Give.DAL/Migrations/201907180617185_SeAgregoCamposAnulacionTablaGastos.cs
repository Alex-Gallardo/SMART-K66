namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCamposAnulacionTablaGastos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gasto", "Comentario", c => c.String());
            AddColumn("dbo.Gasto", "Anulada", c => c.Boolean(nullable: false));
            AddColumn("dbo.Gasto", "Usr_Anular", c => c.Long());
            CreateIndex("dbo.Gasto", "Usr_Anular");
            AddForeignKey("dbo.Gasto", "Usr_Anular", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Gasto", "Usr_Anular", "dbo.Usuario");
            DropIndex("dbo.Gasto", new[] { "Usr_Anular" });
            DropColumn("dbo.Gasto", "Usr_Anular");
            DropColumn("dbo.Gasto", "Anulada");
            DropColumn("dbo.Gasto", "Comentario");
        }
    }
}
