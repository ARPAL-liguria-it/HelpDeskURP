/*
 * Nome del progetto: HelpDesk URP
 * Copyright (C) 2026 Agenzia regionale per la protezione dell'ambiente ligure
 * ---
 * Questo programma è software libero: puoi ridistribuirlo e/o modificarlo
 * secondo i termini della GNU Affero General Public License pubblicata dalla
 * Free Software Foundation, sia la versione 3 della licenza, sia (a tua scelta)
 * qualsiasi versione successiva.
 * ---
 * Questo programma è distribuito nella speranza che possa essere utile,
 * ma SENZA ALCUNA GARANZIA; senza nemmeno la garanzia implicita di
 * COMMERCIABILITÀ o IDONEITÀ PER UNO SCOPO PARTICOLARE. Vedi la
 * GNU Affero General Public License per ulteriori dettagli.
 * ---
 * Dovresti aver ricevuto una copia della GNU Affero General Public License
 * insieme a questo programma. In caso contrario, vedi <https://www.gnu.org/licenses/>.
*/

namespace GestioneURP.Application.Services;

/// <summary>Opzione di selezione con identificativo numerico e descrizione da mostrare nel form.</summary>
public sealed record LookupLongDto(long Id, string Descrizione);

/// <summary>Opzione di selezione con codice testuale e descrizione da mostrare nel form.</summary>
public sealed record LookupStringDto(string Codice, string Descrizione);

/// <summary>
/// Raggruppa gli elenchi necessari al form di inserimento, evitando di esporre le entità persistenti alla pagina.
/// </summary>
public sealed record NuovaSegnalazioneLookupDto(
	IReadOnlyList<LookupLongDto> TipiRichiesta,
	IReadOnlyList<LookupStringDto> TipiSorgente,
	IReadOnlyList<LookupLongDto> TipiAutoreRichiesta,
	IReadOnlyList<LookupLongDto> TipiTematica,
	IReadOnlyList<LookupLongDto> TipiLuogo,
	IReadOnlyList<LookupStringDto> TipiStruttura);

/// <summary>
/// Trasporta i dati iniziali di una richiesta o di un reclamo. Il tipo determina quali campi
/// vengono utilizzati dal servizio; i nuovi allegati sono passati separatamente.
/// </summary>
public sealed record NuovaSegnalazioneInputDto(
	string TipoSegnalazione,
	long? IdTipoRichiesta,
	string? Sorgente,
	string? Cognome,
	string? Nome,
	string? Email,
	string? Pec,
	string? Telefono,
	string? RagioneSociale,
	long? IdTipoAutoreRichiesta,
	long? IdTipoTematica,
	long? IdTipoLuogo,
	string? ComuneLuogo,
	string? Indirizzo,
	string? Testo,
	string? CodiceStruttura);