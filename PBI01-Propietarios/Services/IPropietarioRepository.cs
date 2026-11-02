using PBI01.Propietarios.Models;

namespace PBI01.Propietarios.Services;

/// <summary>
/// Contrato de almacenamiento de propietarios. Hoy lo implementa una lista en memoria;
/// cuando exista la base de datos se crea otra implementación sin cambiar el controlador.
/// </summary>
public interface IPropietarioRepository
{
    IReadOnlyList<Propietario> ObtenerTodos();
    Propietario? ObtenerPorId(Guid id);
    bool ExisteIdentificacion(string identificacion, Guid? excluirId = null);
    void Agregar(Propietario propietario);
    bool Actualizar(Propietario propietario);
    bool Eliminar(Guid id);
}
