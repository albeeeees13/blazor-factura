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


public async Task<List<DatoReporte>> ObtenerTopArticulosAsync()
{
    var lista = new List<DatoReporte>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = @"
        SELECT Descripcion, SUM(Cantidad) as TotalVendido
        FROM Articulos
        GROUP BY Descripcion
        ORDER BY TotalVendido DESC
        LIMIT 5";

    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        lista.Add(new DatoReporte { 
            Etiqueta = lector.GetString(0), 
            Valor = lector.GetDecimal(1) 
        });
    }
    return lista;
}

public async Task<List<DatoReporte>> ObtenerMejoresMesesAsync()
{
    var lista = new List<DatoReporte>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    // Agrupamos por Año-Mes
    comando.CommandText = @"
        SELECT strftime('%Y-%m', Fecha) as Mes, SUM(Total) as TotalVentas
        FROM Facturas
        GROUP BY Mes
        ORDER BY TotalVentas DESC
        LIMIT 5";

    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        lista.Add(new DatoReporte { 
            Etiqueta = lector.GetString(0), 
            Valor = lector.GetDecimal(1) 
        });
    }
    return lista;
}

public async Task<List<DatoReporte>> ObtenerTopClientesAsync()
{
    var lista = new List<DatoReporte>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = @"
        SELECT NombreCliente, SUM(Total) as TotalGastado
        FROM Facturas
        GROUP BY NombreCliente
        ORDER BY TotalGastado DESC
        LIMIT 5";

    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        lista.Add(new DatoReporte { 
            Etiqueta = lector.GetString(0), 
            Valor = lector.GetDecimal(1) 
        });
    }
    return lista;
}

public async Task<decimal> ObtenerTicketPromedioAsync()
{
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = "SELECT IFNULL(AVG(Total), 0) FROM Facturas";
    
    var resultado = await comando.ExecuteScalarAsync();
    return Convert.ToDecimal(resultado);
}

// CONSULTA 5: Ingresos Totales Históricos
public async Task<decimal> ObtenerIngresosTotalesAsync()
{
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = "SELECT IFNULL(SUM(Total), 0) FROM Facturas";
    
    var resultado = await comando.ExecuteScalarAsync();
    return Convert.ToDecimal(resultado);
}


public async Task<List<DatoReporte>> ObtenerUltimasFacturasAsync()
{
    var lista = new List<DatoReporte>();
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = @"
        SELECT NombreCliente || ' (' || date(Fecha) || ')', Total
        FROM Facturas
        ORDER BY Id DESC
        LIMIT 5";

    using var lector = await comando.ExecuteReaderAsync();
    while (await lector.ReadAsync())
    {
        lista.Add(new DatoReporte { 
            Etiqueta = lector.GetString(0), 
            Valor = lector.GetDecimal(1) 
        });
    }
    return lista;
}

public async Task<int> ObtenerCantidadVentasPequeñasAsync()
{
    using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
    await conexion.OpenAsync();
    
    var comando = conexion.CreateCommand();
    comando.CommandText = "SELECT COUNT(*) FROM Facturas WHERE Total < 500";
    
    var resultado = await comando.ExecuteScalarAsync();
    return Convert.ToInt32(resultado);
}

public async Task<List<DatoReporte>> ObtenerProductosMasRentablesAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT Descripcion, SUM(Cantidad * PrecioUnitario) as TotalDinero
                FROM Articulos
                GROUP BY Descripcion
                ORDER BY TotalDinero DESC
                LIMIT 5";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { 
                    Etiqueta = lector.GetString(0), 
                    Valor = lector.GetDecimal(1) 
                });
            }
            return lista;
        }

        // CONSULTA 7: Día de la semana con más ventas
        public async Task<List<DatoReporte>> ObtenerMejorDiaSemanaAsync()
        {
            var lista = new List<DatoReporte>();
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT 
                    CASE strftime('%w', Fecha)
                        WHEN '0' THEN 'Domingo'
                        WHEN '1' THEN 'Lunes'
                        WHEN '2' THEN 'Martes'
                        WHEN '3' THEN 'Miércoles'
                        WHEN '4' THEN 'Jueves'
                        WHEN '5' THEN 'Viernes'
                        WHEN '6' THEN 'Sábado'
                    END as Dia,
                    COUNT(*) as CantidadFacturas
                FROM Facturas
                GROUP BY Dia
                ORDER BY CantidadFacturas DESC";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                lista.Add(new DatoReporte { 
                    Etiqueta = lector.GetString(0), 
                    Valor = lector.GetDecimal(1) // Aquí usamos el conteo como valor
                });
            }
            return lista;
        }

        // CONSULTA 8: Promedio de artículos por factura
        public async Task<decimal> ObtenerPromedioArticulosPorFacturaAsync()
        {
            using var conexion = new SqliteConnection($"Data Source={_rutaDb}");
            await conexion.OpenAsync();
            
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT CAST(COUNT(*) AS REAL) / (SELECT COUNT(*) FROM Facturas) 
                FROM Articulos";
            
            var resultado = await comando.ExecuteScalarAsync();
            if (resultado == DBNull.Value || resultado == null) return 0;
            return Convert.ToDecimal(resultado);
        }

      



    }
}