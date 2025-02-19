using System.ComponentModel.DataAnnotations;

namespace CreazioneListe.Models
{
    public enum EnumCodFornitore
    {
        [Display(Name ="TOMMASIELLO")]
        ACT,
        [Display(Name = "CESSIONI")]
        CANDELA,
        [Display(Name = "DIR1")]
        DIR1,
        [Display(Name = "FORZA")]
        FORZA,
        [Display(Name = "GIAMPAOLO")]
        GIAMPAOLO,
        [Display(Name = "SCHEGIC")]
        SCHEGIC
    }
}
