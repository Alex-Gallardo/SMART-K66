namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaClienteFotografia : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cliente_Fotografia",
                c => new
                    {
                        Fotografia_Id = c.Int(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 200),
                        ContentType = c.String(maxLength: 150),
                        Length = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => new { t.Fotografia_Id, t.Cliente_Id })
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .Index(t => t.Cliente_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente_Fotografia", "Cliente_Id", "dbo.Cliente");
            DropIndex("dbo.Cliente_Fotografia", new[] { "Cliente_Id" });
            DropTable("dbo.Cliente_Fotografia");
        }
    }
}
