using System;
using System.Collections.Generic;

namespace SistemaVeterinariaPBI01
{
    public class Propietario
    {
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }
    }

    internal class Program
    {
        static List<Propietario> listaPropietarios = new List<Propietario>();

        static void Main(string[] args)
        {
            bool salir = false;

            while (!salir)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("   SISTEMA DE GESTION VETERINARIA");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Registrar Propietario (PBI 01)");
                Console.WriteLine("2. Consultar Propietarios Registrados");
                Console.WriteLine("3. Salir");
                Console.WriteLine("========================================");
                Console.Write("Seleccione una opcion: ");

                string opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        RegistrarPropietario();
                        break;
                    case "2":
                        MostrarPropietarios();
                        break;
                    case "3":
                        salir = true;
                        Console.WriteLine("\nSaliendo del sistema...");
                        break;
                    default:
                        Console.WriteLine("\nOpcion no valida. Presione cualquier tecla para continuar.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void RegistrarPropietario()
        {
            Console.Clear();
            Console.WriteLine("--- REGISTRO DE PROPIETARIO ---");

            Propietario nuevoPropietario = new Propietario();

            nuevoPropietario.Identificacion = SolicitarCampo("Identificacion / Cedula: ");
            nuevoPropietario.NombreCompleto = SolicitarCampo("Nombre Completo: ");
            nuevoPropietario.Telefono = SolicitarCampo("Telefono: ");
            nuevoPropietario.Correo = SolicitarCampo("Correo Electronico: ");
            nuevoPropietario.Direccion = SolicitarCampo("Direccion de residencia: ");

            listaPropietarios.Add(nuevoPropietario);

            Console.WriteLine("\n[EXITO] ¡Propietario registrado correctamente!");
            Console.WriteLine("Presione cualquier tecla para volver al menu.");
            Console.ReadKey();
        }

        static string SolicitarCampo(string mensaje)
        {
            string valor = "";
            while (string.IsNullOrWhiteSpace(valor))
            {
                Console.Write(mensaje);
                valor = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(valor))
                {
                    Console.WriteLine(" [Error] Este campo es obligatorio. Intente de nuevo.\n");
                }
            }
            return valor;
        }

        static void MostrarPropietarios()
        {
            Console.Clear();
            Console.WriteLine("--- LISTA DE PROPIETARIOS REGISTRADOS ---");

            if (listaPropietarios.Count == 0)
            {
                Console.WriteLine("No hay propietarios registrados en el sistema.");
            }
            else
            {
                int contador = 1;
                foreach (var prop in listaPropietarios)
                {
                    Console.WriteLine($"\nPropietario #{contador}");
                    Console.WriteLine($"ID/Cedula: {prop.Identificacion}");
                    Console.WriteLine($"Nombre:    {prop.NombreCompleto}");
                    Console.WriteLine($"Telefono:  {prop.Telefono}");
                    Console.WriteLine($"Correo:    {prop.Correo}");
                    Console.WriteLine($"Direccion: {prop.Direccion}");
                    Console.WriteLine("----------------------------------------");
                    contador++;
                }
            }

            Console.WriteLine("\nPresione cualquier tecla para volver al menu.");
            Console.ReadKey();
        }
    }
}