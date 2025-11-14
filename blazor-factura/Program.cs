using blazor_factura.Data; 
using Microsoft.Data.Sqlite;
using blazor_factura.Data; // <-- Si esta línea ya existe, no la repitas

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

string rutaBase = builder.Environment.ContentRootPath;
string nombreDb = "facturas.db";
string rutaCompletaDb = Path.Combine(rutaBase, nombreDb);

builder.Services.AddSingleton<ServicioFacturas>(sp => 
    new ServicioFacturas(rutaCompletaDb)
);

using var conexion = new SqliteConnection($"Data Source={rutaCompletaDb}");
conexion.Open();

var comandoFacturas = conexion.CreateCommand();
comandoFacturas.CommandText = @"
    CREATE TABLE IF NOT EXISTS Facturas (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Fecha TEXT NOT NULL,
        NombreCliente TEXT NOT NULL,
        Total REAL NOT NULL
    );
";
comandoFacturas.ExecuteNonQuery();

var comandoArticulos = conexion.CreateCommand();
comandoArticulos.CommandText = @"
    CREATE TABLE IF NOT EXISTS Articulos (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        FacturaId INTEGER NOT NULL,
        Descripcion TEXT NOT NULL,
        Cantidad INTEGER NOT NULL,
        PrecioUnitario REAL NOT NULL,
        FOREIGN KEY (FacturaId) REFERENCES Facturas(Id) ON DELETE CASCADE 
    );
";
comandoArticulos.ExecuteNonQuery();
// --- FIN: CÓDIGO DE BASE DE DATOS ---

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// --- Mapeo de .NET 7 ---
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
