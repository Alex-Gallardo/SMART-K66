namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoResponsableAprobacionTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Responsable_Aprobacion_Id", c => c.Long());
            CreateIndex("dbo.Pedido_K66", "Responsable_Aprobacion_Id");
            AddForeignKey("dbo.Pedido_K66", "Responsable_Aprobacion_Id", "dbo.Usuario", "Usuario_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_K66", "Responsable_Aprobacion_Id", "dbo.Usuario");
            DropIndex("dbo.Pedido_K66", new[] { "Responsable_Aprobacion_Id" });
            DropColumn("dbo.Pedido_K66", "Responsable_Aprobacion_Id");
        }
    }
}
