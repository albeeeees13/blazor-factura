using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace blazor_factura.Data
{
    public class ServicioFacturas
    {
        private readonly string _rutaDb;

        public ServicioFacturas(string rutaDb)
        {
            _rutaDb = rutaDb;
        }

        // --- 1. GUARDAR ---
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

        // --- 2. REPORTES BÁSICOS (IGNORANDO ARCHIVADAS) ---

        public async Task<List<string>> ObtenerClientesAsync()
        {
            var clientes = new List<string>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Corregido: Ignora archivadas
            comando.CommandText = "SELECT DISTINCT NombreCliente FROM Facturas WHERE Archivada = 0 ORDER BY NombreCliente";
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
            // Corregido: Ignora archivadas
            comando.CommandText = @"
                SELECT strftime('%Y-%m', Fecha) as Periodo, SUM(Total) as TotalGastado
                FROM Facturas
                WHERE NombreCliente = $cliente AND strftime('%Y', Fecha) = $anio AND Archivada = 0
                GROUP BY Periodo
                ORDER BY Periodo DESC";
            comando.Parameters.AddWithValue("$cliente", nombreCliente);
            comando.Parameters.AddWithValue("$anio", anio);
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
            // Corregido: Ignora archivadas
            comando.CommandText = @"
                SELECT DISTINCT strftime('%Y', Fecha) as Anio
                FROM Facturas
                WHERE Archivada = 0
                ORDER BY Anio DESC";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                anios.Add(lector.GetString(0));
            }
            return anios;
        }

        // --- 3. CONSULTAS INTELIGENTES (BI) ---

        public async Task<List<DatoReporte>> ObtenerTopArticulosAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Correcto: Tiene JOIN y alias 'f'
            comando.CommandText = @"
                SELECT a.Descripcion, SUM(a.Cantidad) as TotalVendido
                FROM Articulos a
                JOIN Facturas f ON a.FacturaId = f.Id
                WHERE f.Archivada = 0
                GROUP BY a.Descripcion
                ORDER BY TotalVendido DESC
                LIMIT 5";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<List<DatoReporte>> ObtenerMejoresMesesAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // CORREGIDO: Quité 'f.' porque no hay alias
            comando.CommandText = @"
                SELECT strftime('%Y-%m', Fecha) as Mes, SUM(Total) as TotalVentas
                FROM Facturas
                WHERE Archivada = 0 
                GROUP BY Mes
                ORDER BY TotalVentas DESC
                LIMIT 5";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<List<DatoReporte>> ObtenerTopClientesAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Correcto: Sin alias
            comando.CommandText = @"
                SELECT NombreCliente, SUM(Total) as TotalGastado
                FROM Facturas
                WHERE Archivada = 0
                GROUP BY NombreCliente
                ORDER BY TotalGastado DESC
                LIMIT 5";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<decimal> ObtenerTicketPromedioAsync()
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Corregido: Ignora archivadas
            comando.CommandText = "SELECT IFNULL(AVG(Total), 0) FROM Facturas WHERE Archivada = 0";
            var resultado = await comando.ExecuteScalarAsync();
            return Convert.ToDecimal(resultado);
        }

        public async Task<decimal> ObtenerIngresosTotalesAsync()
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Corregido: Ignora archivadas
            comando.CommandText = "SELECT IFNULL(SUM(Total), 0) FROM Facturas WHERE Archivada = 0";
            var resultado = await comando.ExecuteScalarAsync();
            return Convert.ToDecimal(resultado);
        }

        public async Task<List<DatoReporte>> ObtenerUltimasFacturasAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // CORREGIDO: Quité 'f.'
            comando.CommandText = @"
                SELECT NombreCliente || ' (' || date(Fecha) || ')', Total
                FROM Facturas
                WHERE Archivada = 0
                ORDER BY Id DESC
                LIMIT 5";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<int> ObtenerCantidadVentasPequeñasAsync()
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Corregido: Ignora archivadas
            comando.CommandText = "SELECT COUNT(*) FROM Facturas WHERE Total < 500 AND Archivada = 0";
            var resultado = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        public async Task<List<DatoReporte>> ObtenerProductosMasRentablesAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // Correcto: Tiene JOIN y alias 'f'
            comando.CommandText = @"
                SELECT a.Descripcion, SUM(a.Cantidad * a.PrecioUnitario) as TotalDinero
                FROM Articulos a
                JOIN Facturas f ON a.FacturaId = f.Id
                WHERE f.Archivada = 0
                GROUP BY a.Descripcion
                ORDER BY TotalDinero DESC
                LIMIT 5";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<List<DatoReporte>> ObtenerMejorDiaSemanaAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // CORREGIDO: Quité 'f.' y 'CASE'
            comando.CommandText = @"
                SELECT 
                    CASE strftime('%w', Fecha)
                        WHEN '0' THEN 'Domingo' WHEN '1' THEN 'Lunes' WHEN '2' THEN 'Martes'
                        WHEN '3' THEN 'Miércoles' WHEN '4' THEN 'Jueves' WHEN '5' THEN 'Viernes'
                        WHEN '6' THEN 'Sábado'
                    END as Dia,
                    COUNT(*) as CantidadFacturas
                FROM Facturas
                WHERE Archivada = 0
                GROUP BY Dia
                ORDER BY CantidadFacturas DESC";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { Etiqueta = lector.GetString(0), Valor = lector.GetDecimal(1) });
            }
            return lista;
        }

        public async Task<decimal> ObtenerPromedioArticulosPorFacturaAsync()
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            // CORREGIDO: Aseguramos que solo cuente activas
            comando.CommandText = @"
                SELECT 
                    (SELECT CAST(COUNT(*) AS REAL) FROM Articulos a JOIN Facturas f ON a.FacturaId = f.Id WHERE f.Archivada = 0) 
                    / 
                    (SELECT COUNT(*) FROM Facturas WHERE Archivada = 0)";
            var resultado = await comando.ExecuteScalarAsync();
            if (resultado == DBNull.Value || resultado == null) return 0;
            return Convert.ToDecimal(resultado);
        }

        // --- 4. GESTIÓN DE ARCHIVO Y BÓVEDA ---

        public async Task<List<Factura>> ObtenerFacturasPorEstadoAsync(bool buscarArchivadas)
        {
            var lista = new List<Factura>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT Id, Fecha, NombreCliente, Total, Archivada FROM Facturas WHERE Archivada = $estado ORDER BY Fecha DESC";
            comando.Parameters.AddWithValue("$estado", buscarArchivadas ? 1 : 0);
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new Factura
                {
                    Id = lector.GetInt32(0),
                    Fecha = DateTime.Parse(lector.GetString(1)),
                    NombreCliente = lector.GetString(2),
                    Archivada = lector.GetBoolean(4)
                });
            }
            return lista;
        }

        public async Task CambiarEstadoArchivoAsync(int idFactura, bool archivar)
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "UPDATE Facturas SET Archivada = $estado WHERE Id = $id";
            comando.Parameters.AddWithValue("$estado", archivar ? 1 : 0);
            comando.Parameters.AddWithValue("$id", idFactura);
            await comando.ExecuteNonQueryAsync();
        }
    }
}