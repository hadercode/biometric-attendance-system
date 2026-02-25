using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LectorHuellas.Core.Models
{
    [Table("turno")]
    public class Shift
    {
        [Key]
        [Column("codigo")]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Column("des")]
        [StringLength(50)]
        public string Description { get; set; } = string.Empty;

        [Column("limite")]
        public int Limit { get; set; }

        [Column("amanecer")]
        public int Dawn { get; set; }

        [Column("tarde")]
        public int Afternoon { get; set; }

        [Column("sobre")]
        public int Over { get; set; }

        [Column("holgura")]
        public int Slack { get; set; }

        [Column("descanso")]
        public int Rest { get; set; }

        [Column("duracion")]
        public int Duration { get; set; }

        [Column("horario")]
        [StringLength(200)]
        public string Schedule { get; set; } = string.Empty;

        [Column("entrada")]
        public int Entry { get; set; }
    }
}
