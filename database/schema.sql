-- Demo schema for an EMPTY, disposable SQL Server database.
-- No operational data or private legacy mappings are included.
CREATE TABLE Assegnazioni (
 PraticaAnno int NOT NULL, PraticaNumero int NOT NULL,
 FornitoreNazione nvarchar(20) NOT NULL, FornitoreCodice int NOT NULL,
 AccertamentoCodice nvarchar(20) NOT NULL, UrgenzaCodice nvarchar(20) NOT NULL,
 Importo decimal(18,2) NOT NULL DEFAULT 0, DataAffidamento int NOT NULL,
 StatoAssegnazione nvarchar(10) NOT NULL, Esportata nchar(1) NOT NULL DEFAULT 'N',
 NomeFileEsportazione nvarchar(255) NULL
);
CREATE TABLE Pratiche (
 AnnoProtocollo int NOT NULL, NumeroProtocollo int NOT NULL,
 TipoPratica nvarchar(20) NOT NULL, ClienteCodice int NOT NULL,
 SoggettoCodice nvarchar(50) NOT NULL, CodiceFiscale nvarchar(32) NULL,
 PartitaIva nvarchar(32) NULL, Denominazione nvarchar(200) NULL,
 ComunePratica nvarchar(100) NULL, PRIMARY KEY (AnnoProtocollo, NumeroProtocollo)
);
CREATE TABLE Fornitori (
 NazioneFornitore nvarchar(20) NOT NULL, CodiceFornitore int NOT NULL,
 TipoFornitore nvarchar(10) NOT NULL, RagioneSocialeFornitore nvarchar(200) NOT NULL,
 PRIMARY KEY (NazioneFornitore, CodiceFornitore, TipoFornitore)
);
CREATE TABLE TipiAccertamento (
 CodiceAccertamento nvarchar(20) PRIMARY KEY, DescrizioneAccertamento nvarchar(200) NOT NULL
);
CREATE TABLE Soggetti (
 CodiceSoggetto nvarchar(50) PRIMARY KEY,
 CognomeRagioneSociale nvarchar(200) NULL, NomeSoggetto nvarchar(100) NULL,
 DataNascita int NULL, CodiceComuneNascita nvarchar(20) NULL,
 ComuneNascita nvarchar(100) NULL, NazioneNascita nvarchar(20) NULL,
 TipoIndirizzo nvarchar(20) NULL, Indirizzo nvarchar(200) NULL,
 NumeroCivico nvarchar(20) NULL, Cap nvarchar(20) NULL,
 CodiceComuneResidenza nvarchar(20) NULL, ComuneResidenza nvarchar(100) NULL,
 ProvinciaResidenza nvarchar(20) NULL, NazioneResidenza nvarchar(20) NULL
);
CREATE TABLE DatoriLavoro (
 LavoratoreCodice nvarchar(50) NOT NULL, TipoContratto nvarchar(20) NULL,
 CodiceFiscaleDatore nvarchar(32) NULL
);
CREATE TABLE Decodifiche (
 TipoDecodifica nvarchar(20) NOT NULL, Lingua nvarchar(20) NOT NULL,
 CodiceDecodifica nvarchar(20) NOT NULL, DescrizioneDecodifica nvarchar(200) NULL,
 ValoreDecodifica nvarchar(100) NULL,
 PRIMARY KEY (TipoDecodifica, Lingua, CodiceDecodifica)
);
CREATE TABLE Annotazioni (
 AnnotazioneSoggetto nvarchar(50) NOT NULL, TipoAnnotazione nvarchar(20) NOT NULL,
 TestoAnnotazione nvarchar(max) NULL, DataAnnotazione int NOT NULL
);
CREATE TABLE RelazioniSoggetti (
 SoggettoOrigine nvarchar(50) NOT NULL, SoggettoCollegato nvarchar(50) NOT NULL,
 TipoRelazione nvarchar(20) NOT NULL, Annullata nchar(1) NOT NULL DEFAULT ' '
);
CREATE TABLE IdentificativiSoggetti (
 IdentificativoSoggetto nvarchar(50) NOT NULL, TipoIdentificativo nvarchar(20) NOT NULL,
 ValoreIdentificativo nvarchar(50) NULL
);
CREATE TABLE Comuni (Comune nvarchar(100) PRIMARY KEY, PROV nvarchar(20) NULL);
CREATE TABLE RegistroFile (
 ID_Reg int IDENTITY PRIMARY KEY, ID_DataReg decimal(8,0) NOT NULL,
 ID_Ora nvarchar(6) NOT NULL, ID_Daf decimal(8,0) NOT NULL, ID_Daf1 decimal(8,0) NOT NULL,
 ID_Operatore nvarchar(100) NOT NULL, ID_NazCor nvarchar(20) NOT NULL,
 ID_CodCor decimal(10,0) NOT NULL, ID_Acc nvarchar(20) NOT NULL,
 ID_Urg nvarchar(20) NOT NULL, ID_NumRic int NOT NULL,
 IF_Formato nvarchar(30) NOT NULL, ID_Path nvarchar(1024) NOT NULL, ID_File nvarchar(255) NOT NULL
);

