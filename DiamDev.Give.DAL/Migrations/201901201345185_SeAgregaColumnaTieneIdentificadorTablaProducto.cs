namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaTieneIdentificadorTablaProducto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Producto", "Tiene_Identificador", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Producto", "Tiene_Identificador");
        }
    }
}
