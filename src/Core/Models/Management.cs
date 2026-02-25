using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LectorHuellas.Core.Models
{
    [Table("gerencia")]
    public class Management
    {
        [Key]
        [Column("codigo")]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Column("gerencia")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
