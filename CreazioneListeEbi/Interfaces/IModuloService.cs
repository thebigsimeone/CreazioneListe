namespace CreazioneListe.Interfaces
{
    public interface IModuloService
    {
        string LeggiModuloTesto(string cOggetto, string cTipo, string tenant);
        string LeggiEredi(string oggetto, string primoRigo, string tenant);
    }
}
