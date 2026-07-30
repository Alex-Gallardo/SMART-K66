namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoColumnaIngresoaMovimientoCategoria : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movimiento_Categoria", "Ingreso", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movimiento_Categoria", "Ingreso");
        }
    }
}
