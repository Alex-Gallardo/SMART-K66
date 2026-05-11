namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoMovimientoCategoriaYRelacionEnMovimiento : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Movimiento_Categoria",
                c => new
                    {
                        Movimiento_Categoria_Id = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 250),
                    })
                .PrimaryKey(t => t.Movimiento_Categoria_Id);
            
            AddColumn("dbo.Movimiento", "Movimiento_Categoria_Id", c => c.Int());
            CreateIndex("dbo.Movimiento", "Movimiento_Categoria_Id");
            AddForeignKey("dbo.Movimiento", "Movimiento_Categoria_Id", "dbo.Movimiento_Categoria", "Movimiento_Categoria_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Movimiento", "Movimiento_Categoria_Id", "dbo.Movimiento_Categoria");
            DropIndex("dbo.Movimiento", new[] { "Movimiento_Categoria_Id" });
            DropColumn("dbo.Movimiento", "Movimiento_Categoria_Id");
            DropTable("dbo.Movimiento_Categoria");
        }
    }
}
