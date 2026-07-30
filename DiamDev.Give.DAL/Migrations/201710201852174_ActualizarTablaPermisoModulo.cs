namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarTablaPermisoModulo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Permiso", "Modulo", c => c.String(nullable: false, maxLength: 200));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Permiso", "Modulo");
        }
    }
}
