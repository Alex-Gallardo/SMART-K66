namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaClienteContacto : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cliente_Contacto",
                c => new
                    {
                        Contacto_Id = c.Int(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Departamento_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Telefono = c.String(),
                        Celular = c.String(),
                        Correo = c.String(),
                        Notas = c.String(),
                    })
                .PrimaryKey(t => new { t.Contacto_Id, t.Cliente_Id })
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Departamento", t => t.Departamento_Id, cascadeDelete: true)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Departamento_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente_Contacto", "Departamento_Id", "dbo.Departamento");
            DropForeignKey("dbo.Cliente_Contacto", "Cliente_Id", "dbo.Cliente");
            DropIndex("dbo.Cliente_Contacto", new[] { "Departamento_Id" });
            DropIndex("dbo.Cliente_Contacto", new[] { "Cliente_Id" });
            DropTable("dbo.Cliente_Contacto");
        }
    }
}
