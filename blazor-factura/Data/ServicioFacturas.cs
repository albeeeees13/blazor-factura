using Microsoft.Data.Sqlite;

// Esta línea es la que define el namespace
namespace blazor_factura.Data
{
    public class ServicioFacturas
    {
        private readonly string _rutaDb;

        public ServicioFacturas(string rutaDb)
        {
            _rutaDb = rutaDb;
        }

        public async Task GuardarFacturaAsync(Factura factura)
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();
            try
            {
                var comandoFactura = conexion.CreateCommand();
                comandoFactura.Transaction = transaccion;
                comandoFactura.CommandText = 
                    @"INSERT INTO Facturas (Fecha, NombreCliente, Total) 
                      VALUES ($fecha, $cliente, $total)";
                comandoFactura.Parameters.AddWithValue("$fecha", factura.Fecha.ToString("o")); 
                comandoFactura.Parameters.AddWithValue("$cliente", factura.NombreCliente);
                comandoFactura.Parameters.AddWithValue("$total", factura.Total);
                await comandoFactura.ExecuteNonQueryAsync();

                var comandoId = conexion.CreateCommand();
                comandoId.Transaction = transaccion;
                comandoId.CommandText = "SELECT last_insert_rowid()";
                var nuevoFacturaId = Convert.ToInt64(await comandoId.ExecuteScalarAsync());

                foreach (var articulo in factura.Articulos)
                {
                    var comandoArticulo = conexion.CreateCommand();
                    comandoArticulo.Transaction = transaccion;
                    comandoArticulo.CommandText = 
                        @"INSERT INTO Articulos (FacturaId, Descripcion, Cantidad, PrecioUnitario)
                          VALUES ($facturaId, $desc, $cant, $precio)";
                    comandoArticulo.Parameters.AddWithValue("$facturaId", nuevoFacturaId);
                    comandoArticulo.Parameters.AddWithValue("$desc", articulo.Descripcion);
                    comandoArticulo.Parameters.AddWithValue("$cant", articulo.Cantidad);
                    comandoArticulo.Parameters.AddWithValue("$precio", articulo.PrecioUnitario);
                    await comandoArticulo.ExecuteNonQueryAsync();
                }
                await transaccion.CommitAsync();
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();
                throw; 
            }
        }
    }
}