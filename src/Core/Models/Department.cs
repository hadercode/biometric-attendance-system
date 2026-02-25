using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LectorHuellas.Core.Models
{
    [Table("departamento")]
    public class Department
    {
        [Key]
        [Column("codigo")]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Column("dpto")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
