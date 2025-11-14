using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<List<string>> ObtenerClientesAsync()
        {
            var clientes = new List<string>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();

            var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT DISTINCT NombreCliente FROM Facturas ORDER BY NombreCliente";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                clientes.Add(lector.GetString(0));
            }
            return clientes;
        }
       public async Task<List<ResumenGasto>> ObtenerGastoMensualAsync(string nombreCliente, string anio)
{
    var resumen = new List<ResumenGasto>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = @"
        SELECT strftime('%Y-%m', Fecha) as Periodo, SUM(Total) as TotalGastado
        FROM Facturas
        WHERE NombreCliente = $cliente AND strftime('%Y', Fecha) = $anio
        GROUP BY Periodo
        ORDER BY Periodo DESC";
    
    comando.Parameters.AddWithValue("$cliente", nombreCliente);
    comando.Parameters.AddWithValue("$anio", anio); // <-- El nuevo parámetro
    
    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        resumen.Add(new ResumenGasto
        {
            Periodo = lector.GetString(0),
            TotalGastado = lector.GetDecimal(1)
        });
    }
    return resumen;
}
        public async Task<List<ResumenGasto>> ObtenerGastoAnualPorClienteAsync(string nombreCliente)
        {
            var resumen = new List<ResumenGasto>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
        SELECT strftime('%Y', Fecha) as Periodo, SUM(Total) as TotalGastado
        FROM Facturas
        WHERE NombreCliente = $cliente
        GROUP BY Periodo
        ORDER BY Periodo DESC";

            comando.Parameters.AddWithValue("$cliente", nombreCliente);

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                resumen.Add(new ResumenGasto
                {
                    Periodo = lector.GetString(0),
                    TotalGastado = lector.GetDecimal(1)
                });
            }
            return resumen;
        }

        public async Task<List<string>> ObtenerAniosDisponiblesAsync()
{
    var anios = new List<string>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    // 'strftime' es una función de SQLite para formatear fechas
    comando.CommandText = @"
        SELECT DISTINCT strftime('%Y', Fecha) as Anio
        FROM Facturas
        ORDER BY Anio DESC";
    
    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        anios.Add(lector.GetString(0));
    }
    return anios;
}


    }
}