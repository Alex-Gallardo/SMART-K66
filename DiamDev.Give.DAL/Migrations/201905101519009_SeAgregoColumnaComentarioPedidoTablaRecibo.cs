namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaComentarioPedidoTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Comentario_Pedido", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Comentario_Pedido");
        }
    }
}
