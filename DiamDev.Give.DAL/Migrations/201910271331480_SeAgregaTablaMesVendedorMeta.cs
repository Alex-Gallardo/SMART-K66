namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaMesVendedorMeta : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Vendedor_Meta",
                c => new
                    {
                        Meta_Id = c.Guid(nullable: false, identity: true),
                        Vendedor_Id = c.Long(nullable: false),
                        Mes_Id = c.Int(nullable: false),
                        Anio = c.Int(nullable: false),
                        Monto_Mensual_Meta = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Monto_Mensual_Real = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Responsable_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Meta_Id)
                .ForeignKey("dbo.Mes", t => t.Mes_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Vendedor_Id)
                .Index(t => t.Mes_Id)
                .Index(t => t.Responsable_Id);
            
            CreateTable(
                "dbo.Mes",
                c => new
                    {
                        Mes_Id = c.Int(nullable: false),
                        Nombre = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Mes_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Vendedor_Meta", "Vendedor_Id", "dbo.Vendedor");
            DropForeignKey("dbo.Vendedor_Meta", "Responsable_Id", "dbo.Usuario");
            DropForeignKey("dbo.Vendedor_Meta", "Mes_Id", "dbo.Mes");
            DropIndex("dbo.Vendedor_Meta", new[] { "Responsable_Id" });
            DropIndex("dbo.Vendedor_Meta", new[] { "Mes_Id" });
            DropIndex("dbo.Vendedor_Meta", new[] { "Vendedor_Id" });
            DropTable("dbo.Mes");
            DropTable("dbo.Vendedor_Meta");
        }
    }
}
