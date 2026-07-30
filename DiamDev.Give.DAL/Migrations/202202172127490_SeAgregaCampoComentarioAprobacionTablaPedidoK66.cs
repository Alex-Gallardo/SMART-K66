namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoComentarioAprobacionTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Comentario_Aprobacion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_K66", "Comentario_Aprobacion");
        }
    }
}
