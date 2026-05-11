namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaRecioDelivery : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Recibo_Delivery",
                c => new
                    {
                        Recibo_Id = c.Long(nullable: false),
                        Operado = c.Boolean(nullable: false),
                        Fecha_Operado = c.DateTime(),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Recibo_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Recibo_Delivery");
        }
    }
}
