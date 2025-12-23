using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintSpectator.Shared.Enums;

namespace PrintSpectator.Shared.Models
{
    public class PrintSession
    {
        public Guid Id { get; set; }

        /// <summary>
        /// ID проекта из таблицы Projects (ProjectInfo)
        /// </summary>
        public int ProjectInfoId { get; set; }

        /// <summary>
        /// Название проекта (сохраняем для истории)
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Дата и время начала печати
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Дата и время завершения печати (null если не завершена)
        /// </summary>
        public DateTime? FinishedAt { get; set; }

        /// <summary>
        /// Текущий статус сессии
        /// </summary>
        public ProjectStatus Status { get; set; }

        /// <summary>
        /// Общее количество слоёв в проекте
        /// </summary>
        public int TotalLayers { get; set; }

        /// <summary>
        /// Номер последнего успешно напечатанного слоя
        /// </summary>
        public int LastCompletedLayer { get; set; }

        /// <summary>
        /// ID пользователя, запустившего печать
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Имя пользователя (сохраняем для истории)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Состояния слоёв в этой сессии
        /// </summary>
        public virtual ICollection<LayerState> LayerStates { get; set; } = new List<LayerState>();

        public bool ShowOn { get; set; }
    }
}
