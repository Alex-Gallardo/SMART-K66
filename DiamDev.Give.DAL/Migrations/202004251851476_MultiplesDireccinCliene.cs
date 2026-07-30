namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MultiplesDireccinCliene : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DireccionCliente",
                c => new
                    {
                        Direccion_Id = c.Int(nullable: false, identity: true),
                        Direccion = c.String(nullable: false, maxLength: 300),
                        Latitud = c.Decimal(precision: 18, scale: 2),
                        Longitud = c.Decimal(precision: 18, scale: 2),
                        Cliente_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Direccion_Id)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .Index(t => t.Cliente_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DireccionCliente", "Cliente_Id", "dbo.Cliente");
            DropIndex("dbo.DireccionCliente", new[] { "Cliente_Id" });
            DropTable("dbo.DireccionCliente");
        }
    }
}
