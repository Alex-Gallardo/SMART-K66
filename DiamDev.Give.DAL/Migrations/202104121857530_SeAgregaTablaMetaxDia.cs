namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaMetaxDia : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Vendedor_Meta_x_Dia",
                c => new
                    {
                        Meta_Id = c.Guid(nullable: false, identity: true),
                        Vendedor_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Monto_x_Dia = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Responsable_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Meta_Id)
                .ForeignKey("dbo.Usuario", t => t.Responsable_Id, cascadeDelete: true)
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Vendedor_Id)
                .Index(t => t.Responsable_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Vendedor_Meta_x_Dia", "Vendedor_Id", "dbo.Vendedor");
            DropForeignKey("dbo.Vendedor_Meta_x_Dia", "Responsable_Id", "dbo.Usuario");
            DropIndex("dbo.Vendedor_Meta_x_Dia", new[] { "Responsable_Id" });
            DropIndex("dbo.Vendedor_Meta_x_Dia", new[] { "Vendedor_Id" });
            DropTable("dbo.Vendedor_Meta_x_Dia");
        }
    }
}
