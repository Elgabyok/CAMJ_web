using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

public class PrincipalModel : PageModel
{
    
    private readonly IConfiguration _configuration;

    public PrincipalModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DataTable Movimientos { get; set; } = new DataTable();
    public List<SelectListItem> AniosDisponibles { get; set; } = new List<SelectListItem>();
    
    [BindProperty(SupportsGet = true)]
    public string AnioSeleccionado { get; set; } = "TODOS";
    public string MatriculaUsuario { get; set; } = "Sin matrícula";
    public string NombreUsuario { get; set; } = "Usuario desconocido";



    public void OnGet()
    {
        Console.WriteLine("🚀 Principal.cshtml ha recibido la solicitud inmediatamente");
        
        MatriculaUsuario = HttpContext.Session.GetString("matricula") ?? "Sin matrícula";
        NombreUsuario = HttpContext.Session.GetString("nombre") ?? "Usuario desconocido";

        Console.WriteLine($"Accediendo con matrícula: {MatriculaUsuario} - Nombre: {NombreUsuario}");
        
        CargarAnios();
        CargarMovimientos();        
    }

      
    private void CargarAnios()
    {
        AniosDisponibles.Add(new SelectListItem { Value = "TODOS", Text = "TODOS" });
        for (int i = DateTime.Now.Year; i >= 2024; i--)
        {
            AniosDisponibles.Add(new SelectListItem { Value = i.ToString(), Text = i.ToString() });
        }
    }

    private async Task CargarMovimientosConReintento()
    {
        int intentosMaximos = 2;
        int intentoActual = 0;
        bool exito = false;

        while (!exito && intentoActual < intentosMaximos)
        {
            intentoActual++;
            try
            {
                CargarMovimientos();
                exito = true;
            }
            catch (Exception)
            {               
                if (intentoActual >= intentosMaximos)
                {
                    throw; // Rethrow en el último intento
                }
                // Pequeña pausa antes del reintento
                await Task.Delay(100);
            }
        }
    }
    public int CantidadAportes { get; set; }
    public double TotalImportePagado { get; set; }

    private void CargarMovimientos()
    {
        
        string cadenaConexion = _configuration.GetConnectionString("SupabaseConnection") ?? string.Empty;

        using (var conexion = new NpgsqlConnection(cadenaConexion))
        {
            conexion.Open();

            string matriculaUsuario = HttpContext.Session.GetString("matricula") ?? "Sin matrícula";

            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conexion;
                cmd.CommandTimeout = 120;

                if (!string.IsNullOrEmpty(AnioSeleccionado) && AnioSeleccionado != "TODOS")
                {
                    DateTime fechaInicio = new DateTime(int.Parse(AnioSeleccionado), 1, 1);
                    DateTime fechaFin = new DateTime(int.Parse(AnioSeleccionado), 12, 31);

                    Console.WriteLine($"Filtrando por año: {AnioSeleccionado}, desde {fechaInicio} hasta {fechaFin}");

                    cmd.CommandText = @"SELECT fecha, ""idComprobante"", caratula, 
                                          COALESCE(expediente, '') as expediente, 
                                          importe, estado  
                                          FROM movimientos  
                                          WHERE matricula = @matricula  
                                          AND fecha BETWEEN @fechaInicio AND @fechaFin";

                    cmd.Parameters.AddWithValue("@matricula", matriculaUsuario);
                    cmd.Parameters.Add("@fechaInicio", NpgsqlTypes.NpgsqlDbType.Date).Value = fechaInicio;
                    cmd.Parameters.Add("@fechaFin", NpgsqlTypes.NpgsqlDbType.Date).Value = fechaFin;
                }
                else
                {
                    Console.WriteLine("Mostrando todos los registros sin filtro de año.");
                    cmd.CommandText = @"SELECT fecha, ""idComprobante"", caratula, 
                                          COALESCE(expediente, '') as expediente, 
                                          importe, estado  
                                          FROM movimientos  
                                          WHERE matricula = @matricula";

                    cmd.Parameters.AddWithValue("@matricula", matriculaUsuario);
                }

                // 🛠 Restablecer valores antes de hacer los cálculos
                CantidadAportes = 0;
                TotalImportePagado = 0;
                
                using (var reader = cmd.ExecuteReader())
                {
                    Movimientos = new DataTable();
                    Movimientos.Columns.Add("fecha", typeof(DateTime));
                    Movimientos.Columns.Add("idComprobante", typeof(long));
                    Movimientos.Columns.Add("caratula", typeof(string));
                    Movimientos.Columns.Add("expediente", typeof(string));
                    Movimientos.Columns.Add("importe", typeof(double));
                    Movimientos.Columns.Add("estado", typeof(string));

                    Console.WriteLine("Consulta ejecutada, procesando resultados...");

                    while (reader.Read())
                    {
                        var row = Movimientos.NewRow();
                        row["fecha"] = reader["fecha"];
                        row["idComprobante"] = reader["idComprobante"];
                        row["caratula"] = reader["caratula"];
                        row["expediente"] = reader["expediente"];
                        row["importe"] = reader["importe"];
                        row["estado"] = reader["estado"];
                        Movimientos.Rows.Add(row);

                        // 🔥 **Ahora sumamos solo los registros que tienen estado "PAGADO"**
                        if (row["estado"].ToString() == "PAGADO")
                        {
                            CantidadAportes++;
                            TotalImportePagado += Convert.ToDouble(row["importe"]);
                        }
                    }

                    Console.WriteLine($"Aportes {AnioSeleccionado}: {CantidadAportes}");
                    Console.WriteLine($"Total {AnioSeleccionado}: {TotalImportePagado}");
                }
            }
        }
    }
}