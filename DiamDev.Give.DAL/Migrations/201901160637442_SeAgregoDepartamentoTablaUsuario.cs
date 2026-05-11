namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoDepartamentoTablaUsuario : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuario", "Departamento_Id", c => c.Long());
            CreateIndex("dbo.Usuario", "Departamento_Id");
            AddForeignKey("dbo.Usuario", "Departamento_Id", "dbo.Departamento", "Departamento_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Usuario", "Departamento_Id", "dbo.Departamento");
            DropIndex("dbo.Usuario", new[] { "Departamento_Id" });
            DropColumn("dbo.Usuario", "Departamento_Id");
        }
    }
}
