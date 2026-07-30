namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaMesaRecibo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Mesa_Recibo",
                c => new
                    {
                        Mesa_Id = c.Long(nullable: false),
                        Recibo_Id = c.Long(nullable: false),
                        Pendiente_Pago = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.Mesa_Id, t.Recibo_Id })
                .ForeignKey("dbo.Mesa", t => t.Mesa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Recibo", t => t.Recibo_Id, cascadeDelete: true)
                .Index(t => t.Mesa_Id)
                .Index(t => t.Recibo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Mesa_Recibo", "Recibo_Id", "dbo.Recibo");
            DropForeignKey("dbo.Mesa_Recibo", "Mesa_Id", "dbo.Mesa");
            DropIndex("dbo.Mesa_Recibo", new[] { "Recibo_Id" });
            DropIndex("dbo.Mesa_Recibo", new[] { "Mesa_Id" });
            DropTable("dbo.Mesa_Recibo");
        }
    }
}
