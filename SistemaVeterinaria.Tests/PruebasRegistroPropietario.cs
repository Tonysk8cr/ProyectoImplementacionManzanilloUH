using System;
using System.IO;
using System.Reflection;
using SistemaVeterinariaPBI01;
using Xunit;

namespace SistemaVeterinaria.Tests;

public class PruebasRegistroPropietario : IDisposable
{
    private readonly TextReader _entradaOriginal = Console.In;
    private readonly TextWriter _salidaOriginal = Console.Out;
    private readonly StringWriter _salida = new StringWriter();

    public PruebasRegistroPropietario()
    {
        Console.SetOut(_salida);
    }

    public void Dispose()
    {
        Console.SetIn(_entradaOriginal);
        Console.SetOut(_salidaOriginal);
    }

    // Llama al metodo SolicitarCampo del Program sin modificar el codigo del PBI-01
    private static string SolicitarCampo(string mensaje)
    {
        Type programa = typeof(Propietario).Assembly.GetType("SistemaVeterinariaPBI01.Program")!;
        MethodInfo metodo = programa.GetMethod("SolicitarCampo", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)metodo.Invoke(null, new object[] { mensaje })!;
    }

    // Escenario satisfactorio
    [Fact]
    public void Registro_ConDatosValidos_GuardaTodosLosCampos()
    {
        Console.SetIn(new StringReader("1-2345-6789\nJuan Perez\n8888-8888\njuan@correo.com\nSan Jose\n"));

        var propietario = new Propietario
        {
            Identificacion = SolicitarCampo("Identificacion / Cedula: "),
            NombreCompleto = SolicitarCampo("Nombre Completo: "),
            Telefono = SolicitarCampo("Telefono: "),
            Correo = SolicitarCampo("Correo Electronico: "),
            Direccion = SolicitarCampo("Direccion de residencia: ")
        };

        Assert.Equal("1-2345-6789", propietario.Identificacion);
        Assert.Equal("Juan Perez", propietario.NombreCompleto);
        Assert.Equal("8888-8888", propietario.Telefono);
        Assert.Equal("juan@correo.com", propietario.Correo);
        Assert.Equal("San Jose", propietario.Direccion);
        Assert.DoesNotContain("[Error]", _salida.ToString());
    }

    // Escenario de validacion/error
    [Fact]
    public void Registro_ConCampoVacio_MuestraErrorYVuelveAPedirElDato()
    {
        Console.SetIn(new StringReader("\n   \nJuan Perez\n"));

        string resultado = SolicitarCampo("Nombre Completo: ");

        Assert.Equal("Juan Perez", resultado);
        Assert.Contains("[Error] Este campo es obligatorio", _salida.ToString());
    }
}