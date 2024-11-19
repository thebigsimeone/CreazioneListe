namespace CreazioneListe.Interfaces
{
    public interface IModuloService
    {
        string LeggiModuloTesto(string cOggetto, string cTipo, string tenant);
        string LeggiEredi(string oggetto, string primoRigo, string tenant);
        void AggiornaFileCorrispondenti(int annoProt, int numeroProt, string codAcc, string codUrg, string nazCor, string codCor, string oFile, string tenant);

    }
}
