namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaVendedorEscala : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Vendedor_Escala",
                c => new
                    {
                        Escala_Id = c.Int(nullable: false),
                        Vendedor_Id = c.Long(nullable: false),
                        Inicio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fin = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Porcentaje = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Escala_Id, t.Vendedor_Id })
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Vendedor_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Vendedor_Escala", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Vendedor_Escala", new[] { "Vendedor_Id" });
            DropTable("dbo.Vendedor_Escala");
        }
    }
}
