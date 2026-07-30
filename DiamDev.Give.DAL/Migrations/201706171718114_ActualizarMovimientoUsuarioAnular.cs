namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarMovimientoUsuarioAnular : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Movimiento", "Usr_Creo", "dbo.Usuario");
            AddColumn("dbo.Movimiento", "Anulada", c => c.Boolean(nullable: false));
            AddColumn("dbo.Movimiento", "Usr_Anular", c => c.Long());
            AddColumn("dbo.Movimiento", "Fecha_Anular", c => c.DateTime());
            CreateIndex("dbo.Movimiento", "Usr_Anular");
            AddForeignKey("dbo.Movimiento", "Usr_Anular", "dbo.Usuario", "Usuario_Id");
            AddForeignKey("dbo.Movimiento", "Usr_Creo", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Movimiento", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Movimiento", "Usr_Anular", "dbo.Usuario");
            DropIndex("dbo.Movimiento", new[] { "Usr_Anular" });
            DropColumn("dbo.Movimiento", "Fecha_Anular");
            DropColumn("dbo.Movimiento", "Usr_Anular");
            DropColumn("dbo.Movimiento", "Anulada");
            AddForeignKey("dbo.Movimiento", "Usr_Creo", "dbo.Usuario", "Usuario_Id", cascadeDelete: true);
        }
    }
}
