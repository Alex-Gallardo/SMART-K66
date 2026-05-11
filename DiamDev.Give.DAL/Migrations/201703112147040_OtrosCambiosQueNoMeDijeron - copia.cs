namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OtrosCambiosQueNoMeDijeron : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Personal_Horario", "Entrada", c => c.Time(nullable: false, precision: 7));
            AlterColumn("dbo.Personal_Horario", "Salida", c => c.Time(precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Personal_Horario", "Salida", c => c.DateTime());
            AlterColumn("dbo.Personal_Horario", "Entrada", c => c.DateTime(nullable: false));
        }
    }
}
