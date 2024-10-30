namespace CreazioneListeEbi.Models
{
    public class FormData
    {
        public static string? StaticDaDataAff { get; set; } = DateTime.Now.AddDays(-7).ToString("yyyyMMdd");
        public static string? StaticDataAff { get; set; } = DateTime.Now.ToString("yyyyMMdd");
        public string? NazCor { get; set; }
        public string? CodCor { get; set; }
        public string? DaDataAff { get; set; } = StaticDaDataAff;
        public string? DataAff { get; set; } = StaticDataAff;
        public string? CodAcc { get; set; }
        public string? NumLotto { get; set; }
    }

}
