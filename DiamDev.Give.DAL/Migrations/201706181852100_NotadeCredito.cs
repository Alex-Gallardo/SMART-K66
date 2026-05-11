namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NotadeCredito : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Nota_Credito",
                c => new
                    {
                        Credito_Id = c.Long(nullable: false),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                        Operado = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Anular = c.Long(),
                        Fecha_Anular = c.DateTime(),
                        Comentario = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Credito_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anular)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Anular);
            
            CreateTable(
                "dbo.Nota_Credito_Forma_Pago",
                c => new
                    {
                        Credito_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Credito_Id, t.Forma_Pago_Id })
                .ForeignKey("dbo.Nota_Credito", t => t.Credito_Id, cascadeDelete: true)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .Index(t => t.Credito_Id)
                .Index(t => t.Forma_Pago_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Nota_Credito", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Nota_Credito", "Usr_Anular", "dbo.Usuario");
            DropForeignKey("dbo.Nota_Credito_Forma_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Nota_Credito_Forma_Pago", "Credito_Id", "dbo.Nota_Credito");
            DropIndex("dbo.Nota_Credito_Forma_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Nota_Credito_Forma_Pago", new[] { "Credito_Id" });
            DropIndex("dbo.Nota_Credito", new[] { "Usr_Anular" });
            DropIndex("dbo.Nota_Credito", new[] { "Usr_Creo" });
            DropTable("dbo.Nota_Credito_Forma_Pago");
            DropTable("dbo.Nota_Credito");
        }
    }
}
