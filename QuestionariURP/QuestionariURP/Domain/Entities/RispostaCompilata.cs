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
/// Risposta fornita a una singola domanda all'interno di una Compilazione.
///
/// Per domande chiuse: OpzioneRispostaId e CodiceNumericoSnapshot sono valorizzati;
/// TestoRisposta è null.
///
/// Per domande aperte: TestoRisposta è valorizzato (se fornito);
/// OpzioneRispostaId e CodiceNumericoSnapshot sono null.
///
/// Tabella fisica: URP_HD.QUESTIONARI_RISPOSTA_COMPILATA
/// </summary>
public class RispostaCompilata
{
	public long Id { get; set; }
	public long CompilazioneId { get; set; }
	public long DomandaId { get; set; }

	public long? OpzioneRispostaId { get; set; }

	/// <summary>
	/// Snapshot del CodiceNumerico dell'opzione al momento della compilazione,
	/// indipendente da eventuali modifiche successive alla configurazione.
	/// </summary>
	public long? CodiceNumericoSnapshot { get; set; }

	public string? TestoRisposta { get; set; }
}
