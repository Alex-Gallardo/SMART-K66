namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCamposFechaAnulacionTablaGastos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gasto", "Fecha_Anular", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Gasto", "Fecha_Anular");
        }
    }
}
