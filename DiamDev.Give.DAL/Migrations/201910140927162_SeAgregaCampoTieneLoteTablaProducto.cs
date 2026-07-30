namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoTieneLoteTablaProducto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto", "Tiene_Lote", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Producto", "Tiene_Lote");
        }
    }
}
