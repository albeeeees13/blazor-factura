using System.ComponentModel.DataAnnotations;


namespace blazor_factura.Data
{
   
    public class Articulo
    {
        public int Id { get; set; } 
        public int FacturaId { get; set; } 
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Descripcion { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Cantidad { get; set; } = 1;
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser positivo.")]
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}