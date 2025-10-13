using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalia_.Models
{
    public class Attendant
    {
        public int Id { get; set; } // Identificador único (pode vir do backend ou ser gerado localmente)
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true; // útil pra filtrar
    }
}
