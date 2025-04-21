using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Microsoft.AspNetCore.Http;

namespace CAMJ_web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Clave { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Clave))
            {
                ErrorMessage = "Ingrese su correo y clave.";
                return Page();
            }            

            string cadenaConexion = _configuration.GetConnectionString("SupabaseConnection") ?? string.Empty;

            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();

                string query = "SELECT matricula, nombre FROM usuarios WHERE email = @Email AND clave = @Clave";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Clave", Clave);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string matricula = reader["matricula"].ToString();
                            string nombre = reader["nombre"].ToString();

                            HttpContext.Session.SetString("matricula", matricula);
                            HttpContext.Session.SetString("nombre", nombre);

                            Console.WriteLine($"Login exitoso - Matricula: {matricula}, Nombre: {nombre}");
                            

                            HttpContext.Session.Remove("intentosLogin"); // 🔥 Limpiar el contador tras éxito

                            Console.WriteLine("🚀 Backend ha enviado redirección a Principal...");
                            return Redirect("/Principal");
                        }
                        else
                        {
                            ErrorMessage = "Credenciales incorrectas. Intente nuevamente.";
                            return Page();
                        }
                    }
                }
            }
        }
    }
}