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

namespace QuestionariURP.Domain.Entities;

/// <summary>
/// Opzione di risposta di una DomandaChiusa.
/// Entità in sola lettura per questa applicazione.
/// Tabella fisica: URP_HD.QUESTIONARI_OPZIONE_RISPOSTA
/// </summary>
public class OpzioneRisposta
{
	public long Id { get; set; }
	public long DomandaId { get; set; }
	public int Posizione { get; set; }

	/// <summary>
	/// Codice numerico usato per analisi statistiche esterne.
	/// Univoco tra le opzioni della stessa domanda.
	/// </summary>
	public long CodiceNumerico { get; set; }

	public string Testo { get; set; } = string.Empty;
}
