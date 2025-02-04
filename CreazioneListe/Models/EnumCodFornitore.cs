using System.ComponentModel.DataAnnotations;

namespace CreazioneListe.Models
{
    public enum EnumCodFornitore
    {
        [Display(Name ="TOMMASIELLO")]
        ACT,
        [Display(Name = "CESSIONI")]
        CANDELA,
        [Display(Name = "FORZA")]
        FORZA,
        [Display(Name = "GIAMPAOLO")]
        GIAMPAOLO,
        [Display(Name = "SCHEGIC")]
        SCHEGIC
    }
}
