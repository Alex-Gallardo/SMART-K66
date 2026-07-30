namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoCategoriaIdTablaGasto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gasto", "Categoria_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.Gasto", "Categoria_Id");
            AddForeignKey("dbo.Gasto", "Categoria_Id", "dbo.Categoria_Gasto", "Categoria_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Gasto", "Categoria_Id", "dbo.Categoria_Gasto");
            DropIndex("dbo.Gasto", new[] { "Categoria_Id" });
            DropColumn("dbo.Gasto", "Categoria_Id");
        }
    }
}
