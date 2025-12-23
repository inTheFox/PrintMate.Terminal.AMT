using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintSpectator.Shared.Enums;

namespace PrintSpectator.Shared.Models
{
    public class LayerState
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Внешний ключ к сессии печати
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Навигационное свойство к сессии
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual PrintSession Session { get; set; }

        /// <summary>
        /// Номер слоя в проекте (начиная с 0)
        /// </summary>
        public int LayerNumber { get; set; }

        /// <summary>
        /// Текущий статус слоя
        /// </summary>
        public LayerStatus Status { get; set; }

        /// <summary>
        /// Время начала обработки слоя
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Время завершения обработки слоя (null если не завершён)
        /// </summary>
        public DateTime? FinishedAt { get; set; }

        /// <summary>
        /// Была ли опущена платформа
        /// </summary>
        public bool IsPlatformDown { get; set; }

        /// <summary>
        /// Был ли нанесен порошок
        /// </summary>
        public bool IsPowderApplied { get; set; }

        /// <summary>
        /// Было ли начато сканирование
        /// </summary>
        public bool IsMarkingStarted { get; set; }

        /// <summary>
        /// Было ли завершено сканирование
        /// </summary>
        public bool IsMarkingFinished { get; set; }
    }
}
