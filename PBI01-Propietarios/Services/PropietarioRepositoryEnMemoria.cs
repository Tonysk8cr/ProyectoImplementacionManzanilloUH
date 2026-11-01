using PBI01.Propietarios.Models;

namespace PBI01.Propietarios.Services;

/// <summary>Almacén temporal en memoria. Los datos se pierden al apagar la aplicación.</summary>
public class PropietarioRepositoryEnMemoria : IPropietarioRepository
{
    private readonly List<Propietario> _datos = new();
    private readonly object _lock = new();

    public PropietarioRepositoryEnMemoria()
    {
        // Datos de ejemplo para ver la lista al abrir la app por primera vez.
        _datos.Add(new Propietario
        {
            NombreCompleto = "María Rodríguez",
            Identificacion = "1-1234-5678",
            Correo = "maria@example.com",
            Telefono = "8888 1111",
            Direccion = "San José, Costa Rica"
        });
        _datos.Add(new Propietario
        {
            NombreCompleto = "Carlos Jiménez",
            Identificacion = "2-0456-0789",
            Correo = "carlos@example.com",
            Telefono = "8888 2222"
        });
    }

    public IReadOnlyList<Propietario> ObtenerTodos()
    {
        lock (_lock)
        {
            return _datos.OrderBy(p => p.NombreCompleto).ToList();
        }
    }

    public Propietario? ObtenerPorId(Guid id)
    {
        lock (_lock)
        {
            return _datos.FirstOrDefault(p => p.Id == id);
        }
    }

    public bool ExisteIdentificacion(string identificacion, Guid? excluirId = null)
    {
        lock (_lock)
        {
            return _datos.Any(p =>
                string.Equals(p.Identificacion, identificacion, StringComparison.OrdinalIgnoreCase)
                && (excluirId is null || p.Id != excluirId));
        }
    }

    public void Agregar(Propietario propietario)
    {
        lock (_lock)
        {
            propietario.FechaRegistro = DateTime.Now;
            _datos.Add(propietario);
        }
    }

    public bool Actualizar(Propietario propietario)
    {
        lock (_lock)
        {
            var actual = _datos.FirstOrDefault(p => p.Id == propietario.Id);
            if (actual is null) return false;

            actual.NombreCompleto = propietario.NombreCompleto;
            actual.Identificacion = propietario.Identificacion;
            actual.Correo = propietario.Correo;
            actual.Telefono = propietario.Telefono;
            actual.Direccion = propietario.Direccion;
            return true;
        }
    }

    public bool Eliminar(Guid id)
    {
        lock (_lock)
        {
            return _datos.RemoveAll(p => p.Id == id) > 0;
        }
    }
}
