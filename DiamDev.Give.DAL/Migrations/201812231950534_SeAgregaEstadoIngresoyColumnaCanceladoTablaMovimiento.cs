namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaEstadoIngresoyColumnaCanceladoTablaMovimiento : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Movimiento_Estado",
                c => new
                    {
                        Movimiento_Estado_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 250),
                    })
                .PrimaryKey(t => t.Movimiento_Estado_Id);
            
            AddColumn("dbo.Movimiento", "Movimiento_Estado_Id", c => c.Int());
            AddColumn("dbo.Movimiento", "Cancelado", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Movimiento", "Movimiento_Estado_Id");
            AddForeignKey("dbo.Movimiento", "Movimiento_Estado_Id", "dbo.Movimiento_Estado", "Movimiento_Estado_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Movimiento", "Movimiento_Estado_Id", "dbo.Movimiento_Estado");
            DropIndex("dbo.Movimiento", new[] { "Movimiento_Estado_Id" });
            DropColumn("dbo.Movimiento", "Cancelado");
            DropColumn("dbo.Movimiento", "Movimiento_Estado_Id");
            DropTable("dbo.Movimiento_Estado");
        }
    }
}
