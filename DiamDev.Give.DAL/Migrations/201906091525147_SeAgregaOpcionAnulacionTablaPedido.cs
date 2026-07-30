namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaOpcionAnulacionTablaPedido : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido", "Comentario", c => c.String());
            AddColumn("dbo.Pedido", "Anulada", c => c.Boolean(nullable: false));
            AddColumn("dbo.Pedido", "Usr_Anular", c => c.Long());
            AddColumn("dbo.Pedido", "Fecha_Anular", c => c.DateTime());
            CreateIndex("dbo.Pedido", "Usr_Anular");
            AddForeignKey("dbo.Pedido", "Usr_Anular", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido", "Usr_Anular", "dbo.Usuario");
            DropIndex("dbo.Pedido", new[] { "Usr_Anular" });
            DropColumn("dbo.Pedido", "Fecha_Anular");
            DropColumn("dbo.Pedido", "Usr_Anular");
            DropColumn("dbo.Pedido", "Anulada");
            DropColumn("dbo.Pedido", "Comentario");
        }
    }
}
