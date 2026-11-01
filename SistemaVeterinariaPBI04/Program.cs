using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaVeterinariaPBI04
{
    public class Mascota
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public int Edad { get; set; }
        public string IdPropietario { get; set; }
    }

    internal class Program
    {
        // Datos de prueba (luego se reemplaza por la lista real del PBI-02)
        static List<Mascota> listaMascotas = new List<Mascota>
        {
            new Mascota { Id = "M001", Nombre = "Rexy", Especie = "Perro", Raza = "Border Collie", Edad = 3, IdPropietario = "001" },
            new Mascota { Id = "M002", Nombre = "Miau", Especie = "Gato", Raza = "Siames", Edad = 2, IdPropietario = "002" }
        };

        static void Main(string[] args)
        {
            bool salir = false;
            while (!salir)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("   SISTEMA DE GESTION VETERINARIA");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Consultar Mascota (PBI 04)");
                Console.WriteLine("2. Salir");
                Console.WriteLine("========================================");
                Console.Write("Seleccione una opcion: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        ConsultarMascota();
                        break;
                    case "2":
                        salir = true;
                        break;
                    default:
                        Console.WriteLine("\nOpcion no valida. Presione cualquier tecla para salir.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void ConsultarMascota()
        {
            Console.Clear();
            Console.WriteLine("--- CONSULTAR INFORMACION DE MASCOTA ---");

            if (listaMascotas.Count == 0)
            {
                Console.WriteLine("No hay mascotas registradas.");
                Pausa();
                return;
            }

            Console.Write("Ingrese el ID o el nombre de la mascota: ");
            string busqueda = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                Console.WriteLine("\n[Error] Debe ingresar un ID o un nombre.");
                Pausa();
                return;
            }

            var mascota = listaMascotas.FirstOrDefault(m =>
                m.Id.Equals(busqueda.Trim(), StringComparison.OrdinalIgnoreCase) ||
                m.Nombre.Equals(busqueda.Trim(), StringComparison.OrdinalIgnoreCase));

            if (mascota == null)
            {
                Console.WriteLine("\n[Error] No se encontro ninguna mascota con este dato.  Intente nuevamente.");
            }
            else
            {
                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine($"ID:          {mascota.Id}");
                Console.WriteLine($"Nombre:      {mascota.Nombre}");
                Console.WriteLine($"Especie:     {mascota.Especie}");
                Console.WriteLine($"Raza:        {mascota.Raza}");
                Console.WriteLine($"Edad:        {mascota.Edad} año(s)");
                Console.WriteLine($"Propietario: {mascota.IdPropietario}");
                Console.WriteLine("----------------------------------------");
            }
            Pausa();
        }

        static void Pausa()
        {
            Console.WriteLine("\nPresione cualquier tecla para volver al menu.");
            Console.ReadKey();
        }
    }
}