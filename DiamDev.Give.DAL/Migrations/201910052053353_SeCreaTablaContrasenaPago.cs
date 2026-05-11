namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCreaTablaContrasenaPago : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Contrasena_Pago",
                c => new
                    {
                        Contrasena_Id = c.Long(nullable: false),
                        Proveedor_Id = c.Long(nullable: false),
                        Forma_Id = c.Long(nullable: false),
                        Documento = c.String(maxLength: 150),
                        Fecha_Pago = c.DateTime(nullable: false),
                        Comentario = c.String(maxLength: 500),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Usr_Creo = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Contrasena_Id)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Forma_Id)
                .Index(t => t.Usr_Creo);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Contrasena_Pago", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Contrasena_Pago", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Contrasena_Pago", "Forma_Id", "dbo.Forma_Pago");
            DropIndex("dbo.Contrasena_Pago", new[] { "Usr_Creo" });
            DropIndex("dbo.Contrasena_Pago", new[] { "Forma_Id" });
            DropIndex("dbo.Contrasena_Pago", new[] { "Proveedor_Id" });
            DropTable("dbo.Contrasena_Pago");
        }
    }
}
