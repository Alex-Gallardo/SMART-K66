namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaTipoProveedor : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Proveedor_Tipo",
                c => new
                    {
                        Tipo_Id = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
            AddColumn("dbo.Proveedor", "Tipo_Id", c => c.Int());
            CreateIndex("dbo.Proveedor", "Tipo_Id");
            AddForeignKey("dbo.Proveedor", "Tipo_Id", "dbo.Proveedor_Tipo", "Tipo_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proveedor", "Tipo_Id", "dbo.Proveedor_Tipo");
            DropIndex("dbo.Proveedor", new[] { "Tipo_Id" });
            DropColumn("dbo.Proveedor", "Tipo_Id");
            DropTable("dbo.Proveedor_Tipo");
        }
    }
}
