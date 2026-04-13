using System;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Representa um alerta ou notificação gerada pelo sistema para um utilizador.
    /// Serve para avisar sobre contentores cheios, novas ocorrências ou mudanças de estado.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Chave primária da notificação.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID do contentor associado (opcional).
        /// </summary>
        public int? ContainerId { get; set; }

        /// <summary>
        /// Texto principal da notificação que vai aparecer no ecrã do utilizador.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Flag que indica se a notificação já foi aberta ou vista. Usada para controlar o contador de mensagens não lidas.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Regista o momento exato em que o alerta foi criado pelo sistema.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Chave estrangeira do utilizador que deve receber este alerta. 
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o utilizador destinatário.
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// URL para onde o utilizador é redirecionado ao clicar no alerta (ex: detalhes da ocorrência).
        /// </summary>
        public string? LinkUrl { get; set; }

        /// <summary>
        /// Categoria da notificação (ex: "container" ou "occurrence"). 
        /// Serve para o frontend decidir que ícone ou cor mostrar.
        /// </summary>
        public string? NotificationType { get; set; }
    }
}