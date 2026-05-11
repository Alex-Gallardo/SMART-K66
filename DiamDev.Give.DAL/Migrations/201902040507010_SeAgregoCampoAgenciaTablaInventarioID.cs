namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoAgenciaTablaInventarioID : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Producto_Inventario_ID");
            AddColumn("dbo.Producto_Inventario_ID", "Agencia_Id", c => c.Long(nullable: false));
            AddPrimaryKey("dbo.Producto_Inventario_ID", new[] { "Producto_Id", "Agencia_Id", "ID" });
            CreateIndex("dbo.Producto_Inventario_ID", "Agencia_Id");
            AddForeignKey("dbo.Producto_Inventario_ID", "Agencia_Id", "dbo.Agencia", "Agencia_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Producto_Inventario_ID", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Producto_Inventario_ID", new[] { "Agencia_Id" });
            DropPrimaryKey("dbo.Producto_Inventario_ID");
            DropColumn("dbo.Producto_Inventario_ID", "Agencia_Id");
            AddPrimaryKey("dbo.Producto_Inventario_ID", new[] { "Producto_Id", "ID" });
        }
    }
}
