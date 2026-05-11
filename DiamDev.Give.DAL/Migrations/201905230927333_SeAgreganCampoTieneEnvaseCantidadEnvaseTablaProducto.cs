namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCampoTieneEnvaseCantidadEnvaseTablaProducto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto", "Tiene_Envase", c => c.Boolean(nullable: false));
            AddColumn("dbo.Producto", "Cantidad_Envase", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Producto", "Cantidad_Envase");
            DropColumn("dbo.Producto", "Tiene_Envase");
        }
    }
}
