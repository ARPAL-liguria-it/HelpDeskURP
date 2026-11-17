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

/// <summary>
/// Metadati di un allegato mostrati nel dettaglio; la data è ricavata dal file fisico e può non essere disponibile.
/// </summary>
public record AllegatoDettaglioDto(long IdAllegato, string NomeFile, DateTime? DataCaricamento, string? Sorgente);

/// <summary>
/// Riunisce i dati del cittadino, le descrizioni delle classificazioni, gli allegati e la gestione della pratica.
/// Le due flag di visualizzazione indicano quale sezione specifica mostrare per richieste e reclami;
/// i campi non applicabili al tipo di segnalazione possono essere nulli.
/// </summary>
public record SegnalazioneDettaglioDto(
	long Id,
	string? Codice,
	string TipoSegnalazione,
	DateTime? DataInserimento,
	string Tipologia,
	string? Email,
	string? Cognome,
	string? Nome,
	string? Pec,
	string? Categoria,
	string? Telefono,
	bool MostraSezioneRichiesta,
	bool MostraSezioneReclamo,
	string? Tematica,
	string? Luogo,
	string? ComuneSpecifico,
	string? Richiesta,
	string? Oggetto,
	string? Struttura,
	List<AllegatoDettaglioDto> Allegati,
	// Gestione della pratica
	string StatoPratica,
	DateTime? DataPresaInCarico,
	string? OperatorePresaInCarico,
	long? IdOperatorePresaInCarico,
	string? AnnotazioniPresaInCarico,
	DateTime? DataAssegnazioneAltroOperatore,
	string? OperatoreAssegnatario,
	long? IdOperatoreAssegnatario,
	string? AnnotazioniAssegnazione,
	DateTime? DataInoltroAltraStruttura,
	string? StrutturaDestinataria,
	string? CodiceStrutturaDestinataria,
	string? AnnotazioniInoltro,
	DateTime? DataRispostaStruttura,
	string? RispostaDaStruttura,
	DateTime? DataRisposta,
	string? TestoRisposta,
	string? AnnotazioniRisposta,
	DateTime? DataChiusura,
	string? AnnotazioniChiusura,
	DateTime? UltimoAggiornamento,
	string? OperatoreUltimoAggiornamento,
	string? Sorgente,
	string? RagioneSociale,
	string? Indirizzo
);
