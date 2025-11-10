using System.ComponentModel.DataAnnotations;

// Asegúrate de que esta línea exista
namespace blazor_factura.Data
{
    public class Factura
    {
        public int Id { get; set; } 
        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateTime Fecha { get; set; } = DateTime.Now;
        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        public string NombreCliente { get; set; } = string.Empty;
        
        // Esta línea necesita que 'Articulo.cs' exista
        public List<Articulo> Articulos { get; set; } = new();

        public decimal Total => Articulos.Sum(a => a.Subtotal);
    }
}