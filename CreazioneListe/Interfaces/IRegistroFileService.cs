namespace CreazioneListe.Interfaces
{
    public interface IRegistroFileService
    {
        void ScriviRegistroFile(decimal dataAMG, string oraHMS, decimal dataAff, decimal daDataAff, string nazCor, string codCor, string codAcc, string codUrg, int totRic, string xPercorso, string nomeFile, string formato, string operatore, string tenant);
    }
}
