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

namespace GestioneURP.Infrastructure.Persistence;

public class Richiesta
{
	public long IdRichiesta { get; set; }
	public DateTime? DataInserimento { get; set; }
	public long? IdTipoRichiesta { get; set; }
	public long? IdTipoAutoreRichiesta { get; set; }
	public string? CognomeAutore { get; set; }
	public string? NomeAutore { get; set; }
	public string? PecAutore { get; set; }
	public string? EmailAutore { get; set; }
	public string? TelefonoAutore { get; set; }
	public long? IdTipoTematica { get; set; }
	public long? IdTipoLuogo { get; set; }
	public string? ComuneLuogo { get; set; }
	public string? Testo { get; set; }
	public string? Codice { get; set; }
	public string? StatoPratica { get; set; }
	public DateTime? DataPresaInCarico { get; set; }
	public string? OperatorePresaInCarico { get; set; }
	public long? IdOperatorePresaInCarico { get; set; }
	public string? AnnotazioniPresaInCarico { get; set; }
	public DateTime? DataAssegnazioneAltroOperatore { get; set; }
	public string? OperatoreAssegnatario { get; set; }
	public long? IdOperatoreAssegnatario { get; set; }
	public string? AnnotazioniAssegnazione { get; set; }
	public DateTime? DataInoltroAltraStruttura { get; set; }
	public string? StrutturaDestinataria { get; set; }
	public string? AnnotazioniInoltro { get; set; }
	public DateTime? DataRispostaStruttura { get; set; }
	public string? RispostaDaStruttura { get; set; }
	public DateTime? DataRisposta { get; set; }
	public string? TestoRisposta { get; set; }
	public string? AnnotazioniRisposta { get; set; }
	public DateTime? DataChiusura { get; set; }
	public string? AnnotazioniChiusura { get; set; }
	public DateTime? UltimoAggiornamento { get; set; }
	public string? OperatoreUltimoAggiornamento { get; set; }
	public string? Sorgente { get; set; }
	public string? RagioneSociale { get; set; }
	public string? Indirizzo { get; set; }
	public string? Eliminata { get; set; }
	public string? CodiceStrutturaDestinataria { get; set; }

	public ICollection<AllegatoRichiesta> Allegati { get; set; } = new List<AllegatoRichiesta>();
}
