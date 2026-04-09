using EcoCityWaste.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Data
{
    /// <summary>
    /// Contexto da Base de Dados da aplicação. 
    /// Faz a ponte entre os Models e as tabelas do SQL Server através do Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Construtor que recebe a connection string definida no Program.cs.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary> Tabela de Utilizadores e credenciais. </summary>
        public DbSet<User> Users { get; set; }

        /// <summary> Tabela principal dos Contentores instalados no município. </summary>
        public DbSet<Models.Container> Contentores { get; set; }

        /// <summary> Registo histórico de todas as alterações de estado dos contentores. </summary>
        public DbSet<ContainerStatusHistory> ContainerStatusHistories { get; set; }

        /// <summary> Tabela de Alertas e Notificações enviadas aos utilizadores. </summary>
        public DbSet<Notification> Notifications { get; set; }

        /// <summary> Registo de Ocorrências reportadas pelos cidadãos. </summary>
        public DbSet<Occurrence> Occurrences { get; set; }

        /// <summary> Tabela com o planeamento das Rotas de recolha. </summary>
        public DbSet<EcoCityWaste.Models.Route> Routes { get; set; }

        /// <summary> Tabela intermédia que liga Rotas a Contentores. </summary>
        public DbSet<RouteContainer> RouteContainers { get; set; }

        /// <summary> Registo de erros e exceções capturadas pelo sistema para manutenção. </summary>
        public DbSet<FailureLog> FailureLogs { get; set; }

        /// <summary>
        /// Configuração avançada do modelo de dados. 
        /// Aqui definimos as regras de integridade referencial e comportamentos das relações entre tabelas.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da relação Rota/RouteContainer
            // Se uma Rota for eliminada, as linhas correspondentes na tabela intermédia são apagadas automaticamente.
            modelBuilder.Entity<RouteContainer>()
                .HasOne(rc => rc.Route)
                .WithMany(r => r.RouteContainers)
                .HasForeignKey(rc => rc.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração da relação Contentor/RouteContainer
            // Restringimos o delete: um Contentor não pode ser apagado da base de dados se estiver associado a uma Rota existente.
            // Isto evita erros de integridade e perda de histórico de recolhas.
            modelBuilder.Entity<RouteContainer>()
                .HasOne(rc => rc.Container)
                .WithMany()
                .HasForeignKey(rc => rc.ContainerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}