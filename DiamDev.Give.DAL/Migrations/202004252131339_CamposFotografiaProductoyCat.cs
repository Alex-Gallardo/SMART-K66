namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CamposFotografiaProductoyCat : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto", "FotografiaApp", c => c.String());
            AddColumn("dbo.Producto_Categoria", "FotografiaApp", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Producto_Categoria", "FotografiaApp");
            DropColumn("dbo.Producto", "FotografiaApp");
        }
    }
}
